import { FormEvent, useCallback, useEffect, useState } from 'react';
import { PackageCheck, RefreshCw, Truck } from 'lucide-react';
import { Link } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import {
  BusinessRole,
  PurchaseRequestStatus,
  type CurrentMembershipDto,
  type PurchaseRequestFulfillmentQueueDto,
  type PurchaseRequestFulfillmentQueueItemDto,
} from '../types';
import { HttpError, organizationApi, purchaseRequestApi } from '../services/api';
import { getApiErrorMessage } from '../utils/helpers';

function formatMoney(value: number): string {
  return `${value.toFixed(2)} PLN`;
}

function formatDate(value: string): string {
  return new Date(value).toLocaleString();
}

function getStatusLabel(status: PurchaseRequestStatus): string {
  return PurchaseRequestStatus[status] ?? 'Unknown';
}

function isConflict(error: unknown): boolean {
  return error instanceof HttpError && error.status === 409;
}

export default function PurchaseRequestFulfillment() {
  const { user } = useAuth();
  const [membership, setMembership] = useState<CurrentMembershipDto | null>(null);
  const [queue, setQueue] = useState<PurchaseRequestFulfillmentQueueDto | null>(null);
  const [orderNumbers, setOrderNumbers] = useState<Record<string, string>>({});
  const [fulfillmentNotes, setFulfillmentNotes] = useState<Record<string, string>>({});
  const [loadingMembership, setLoadingMembership] = useState(true);
  const [loadingQueue, setLoadingQueue] = useState(false);
  const [busyKey, setBusyKey] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const isAdmin = user?.role === 'Admin';
  const isProcurement = membership?.isActive === true
    && membership.role === BusinessRole.Procurement;
  const canAccessQueue = membership !== null && (isAdmin || isProcurement);

  useEffect(() => {
    let active = true;

    const loadMembership = async () => {
      setLoadingMembership(true);
      setError(null);

      try {
        const response = await organizationApi.getCurrentMembership();
        if (!response.data) {
          throw new Error('Current membership response missing data.');
        }

        if (active) {
          setMembership(response.data);
        }
      } catch (caughtError) {
        if (active) {
          setError(getApiErrorMessage(caughtError, {
            defaultMessage: 'Unable to load the current organization membership.',
          }));
        }
      } finally {
        if (active) {
          setLoadingMembership(false);
        }
      }
    };

    void loadMembership();

    return () => {
      active = false;
    };
  }, []);

  const loadQueue = useCallback(async () => {
    if (!membership || !canAccessQueue) {
      setLoadingQueue(false);
      return;
    }

    setLoadingQueue(true);
    setError(null);

    try {
      const response = await purchaseRequestApi.listFulfillmentQueue(membership.organizationId);
      if (!response.data) {
        throw new Error('Fulfillment queue response missing data.');
      }

      setQueue(response.data);
    } catch (caughtError) {
      setError(getApiErrorMessage(caughtError, {
        defaultMessage: 'Unable to load the Procurement fulfillment queue.',
      }));
    } finally {
      setLoadingQueue(false);
    }
  }, [canAccessQueue, membership]);

  useEffect(() => {
    void loadQueue();
  }, [loadQueue]);

  const handleOrder = async (item: PurchaseRequestFulfillmentQueueItemDto) => {
    if (!membership) {
      return;
    }

    const operationKey = `${item.id}-order`;
    setBusyKey(operationKey);
    setError(null);
    setNotice(null);

    try {
      const response = await purchaseRequestApi.markOrdered(
        membership.organizationId,
        item.id,
        {
          concurrencyStamp: item.concurrencyStamp,
          orderNumber: orderNumbers[item.id]?.trim() || null,
          fulfillmentNote: fulfillmentNotes[item.id]?.trim() || null,
        },
      );
      if (!response.data) {
        throw new Error('Order transition response missing data.');
      }

      setOrderNumbers((current) => {
        const next = { ...current };
        delete next[item.id];
        return next;
      });
      setFulfillmentNotes((current) => {
        const next = { ...current };
        delete next[item.id];
        return next;
      });
      setNotice('Purchase request marked as ordered.');
      await loadQueue();
    } catch (caughtError) {
      if (isConflict(caughtError)) {
        await loadQueue();
        setError('This request changed in another session. The latest fulfillment queue has been loaded.');
      } else {
        setError(getApiErrorMessage(caughtError, {
          defaultMessage: 'Unable to mark the purchase request as ordered.',
        }));
      }
    } finally {
      setBusyKey(null);
    }
  };

  const handleDelivered = async (item: PurchaseRequestFulfillmentQueueItemDto) => {
    if (!membership) {
      return;
    }

    const operationKey = `${item.id}-deliver`;
    setBusyKey(operationKey);
    setError(null);
    setNotice(null);

    try {
      const response = await purchaseRequestApi.markDelivered(
        membership.organizationId,
        item.id,
        {
          concurrencyStamp: item.concurrencyStamp,
          fulfillmentNote: fulfillmentNotes[item.id]?.trim() || null,
        },
      );
      if (!response.data) {
        throw new Error('Delivery transition response missing data.');
      }

      setFulfillmentNotes((current) => {
        const next = { ...current };
        delete next[item.id];
        return next;
      });
      setNotice('Purchase request marked as delivered.');
      await loadQueue();
    } catch (caughtError) {
      if (isConflict(caughtError)) {
        await loadQueue();
        setError('This request changed in another session. The latest fulfillment queue has been loaded.');
      } else {
        setError(getApiErrorMessage(caughtError, {
          defaultMessage: 'Unable to mark the purchase request as delivered.',
        }));
      }
    } finally {
      setBusyKey(null);
    }
  };

  const handleOrderSubmit = (
    event: FormEvent<HTMLFormElement>,
    item: PurchaseRequestFulfillmentQueueItemDto,
  ) => {
    event.preventDefault();
    void handleOrder(item);
  };

  const handleDeliverySubmit = (
    event: FormEvent<HTMLFormElement>,
    item: PurchaseRequestFulfillmentQueueItemDto,
  ) => {
    event.preventDefault();
    void handleDelivered(item);
  };

  if (loadingMembership) {
    return (
      <section className="page-shell">
        <div className="page-state">
          <p>Loading fulfillment workspace...</p>
        </div>
      </section>
    );
  }

  if (!canAccessQueue) {
    return (
      <section className="page-shell">
        <div className="page-state">
          <PackageCheck aria-hidden="true" size={32} />
          <h1>Purchase request fulfillment</h1>
          <p>{error ?? 'An active Procurement membership or platform administrator account is required.'}</p>
          <Link className="button button--ghost" to="/purchase-requests">
            Back to purchase requests
          </Link>
        </div>
      </section>
    );
  }

  return (
    <section className="page-shell purchase-requests-page">
      <header className="page-shell__header">
        <div className="stack stack--tight">
          <p className="eyebrow">Procurement</p>
          <h1>Purchase request fulfillment</h1>
          <p className="page-note">
            Move accepted purchase requests from ordering to delivery.
          </p>
        </div>
        <button
          className="button button--ghost"
          type="button"
          onClick={() => void loadQueue()}
          disabled={loadingQueue}
        >
          <RefreshCw aria-hidden="true" size={17} />
          Refresh
        </button>
      </header>

      {error ? <div className="form__error" role="alert">{error}</div> : null}
      {notice ? <div className="form__success" role="status">{notice}</div> : null}

      <div className="purchase-request-layout">
        <aside className="purchase-request-sidebar">
          <section className="card">
            <div className="purchase-request-section-heading">
              <div>
                <p className="eyebrow">Queue</p>
                <h2>Accepted requests</h2>
              </div>
              <span className="role-badge">{queue?.items.length ?? 0}</span>
            </div>
            <p className="page-note">
              Ordered requests remain here until Procurement records their delivery.
            </p>
          </section>
        </aside>

        <div className="purchase-request-editor">
          <section className="card">
            <div className="purchase-request-heading">
              <div>
                <p className="eyebrow">{isAdmin ? 'Admin' : 'Procurement'}</p>
                <h2>Fulfillment queue</h2>
              </div>
            </div>

            {loadingQueue ? <p className="page-note">Loading fulfillment queue...</p> : null}
            {!loadingQueue && queue?.items.length === 0 ? (
              <p className="page-note">No accepted purchase requests are waiting for fulfillment.</p>
            ) : null}

            <div className="purchase-request-items">
              {queue?.items.map((item) => {
                const orderKey = `${item.id}-order`;
                const deliveryKey = `${item.id}-deliver`;

                return (
                  <article className="purchase-request-item" key={item.id}>
                    <div className="purchase-request-item__details">
                      <div className="purchase-request-heading">
                        <div>
                          <h3>{item.note || 'Purchase request'}</h3>
                          <p>Created {formatDate(item.createdAt)}</p>
                        </div>
                        <span className="role-badge">{getStatusLabel(item.status)}</span>
                      </div>
                      <dl className="purchase-request-item__totals">
                        <div>
                          <dt>Items</dt>
                          <dd>{item.items.length}</dd>
                        </div>
                        <div>
                          <dt>Total</dt>
                          <dd>{formatMoney(item.totalValue)}</dd>
                        </div>
                        {item.fulfillmentOrderNumber ? (
                          <div>
                            <dt>Order</dt>
                            <dd>{item.fulfillmentOrderNumber}</dd>
                          </div>
                        ) : null}
                      </dl>
                      {item.fulfillmentNote ? <p className="page-note">{item.fulfillmentNote}</p> : null}
                      {item.items.length > 0 ? (
                        <ul>
                          {item.items.map((requestItem) => (
                            <li key={requestItem.id}>
                              {requestItem.productName} × {requestItem.quantity}
                            </li>
                          ))}
                        </ul>
                      ) : null}
                    </div>

                    <div className="purchase-request-item__actions">
                      {item.status === PurchaseRequestStatus.Approved ? (
                        <form className="form" onSubmit={(event) => handleOrderSubmit(event, item)}>
                          <div className="field">
                            <label className="field__label" htmlFor={`order-number-${item.id}`}>
                              Order number
                            </label>
                            <input
                              id={`order-number-${item.id}`}
                              value={orderNumbers[item.id] ?? ''}
                              onChange={(event) => setOrderNumbers((current) => ({
                                ...current,
                                [item.id]: event.target.value,
                              }))}
                              maxLength={100}
                            />
                          </div>
                          <div className="field">
                            <label className="field__label" htmlFor={`fulfillment-note-${item.id}`}>
                              Fulfillment note
                            </label>
                            <textarea
                              id={`fulfillment-note-${item.id}`}
                              value={fulfillmentNotes[item.id] ?? ''}
                              onChange={(event) => setFulfillmentNotes((current) => ({
                                ...current,
                                [item.id]: event.target.value,
                              }))}
                              maxLength={1000}
                              rows={3}
                            />
                          </div>
                          <button className="button" type="submit" disabled={busyKey !== null}>
                            <PackageCheck aria-hidden="true" size={17} />
                            {busyKey === orderKey ? 'Marking as ordered...' : 'Mark as ordered'}
                          </button>
                        </form>
                      ) : (
                        <form className="form" onSubmit={(event) => handleDeliverySubmit(event, item)}>
                          <div className="field">
                            <label className="field__label" htmlFor={`delivery-note-${item.id}`}>
                              Delivery note
                            </label>
                            <textarea
                              id={`delivery-note-${item.id}`}
                              value={fulfillmentNotes[item.id] ?? ''}
                              onChange={(event) => setFulfillmentNotes((current) => ({
                                ...current,
                                [item.id]: event.target.value,
                              }))}
                              maxLength={1000}
                              rows={3}
                            />
                          </div>
                          <button className="button" type="submit" disabled={busyKey !== null}>
                            <Truck aria-hidden="true" size={17} />
                            {busyKey === deliveryKey ? 'Marking as delivered...' : 'Mark as delivered'}
                          </button>
                        </form>
                      )}
                    </div>
                  </article>
                );
              })}
            </div>
          </section>
        </div>
      </div>
    </section>
  );
}
