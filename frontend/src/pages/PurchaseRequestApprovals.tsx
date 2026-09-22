import { FormEvent, useCallback, useEffect, useState } from 'react';
import { Check, RefreshCw, WalletCards, XCircle } from 'lucide-react';
import { Link } from 'react-router-dom';
import {
  BusinessRole,
  PurchaseRequestStatus,
  type BranchMonthlyBudgetDto,
  type CurrentMembershipDto,
  type PurchaseRequestApprovalQueueDto,
  type PurchaseRequestApprovalQueueItemDto,
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

function isMissingBudget(error: unknown): boolean {
  return error instanceof HttpError && error.status === 404;
}

export default function PurchaseRequestApprovals() {
  const [membership, setMembership] = useState<CurrentMembershipDto | null>(null);
  const [queue, setQueue] = useState<PurchaseRequestApprovalQueueDto | null>(null);
  const [budget, setBudget] = useState<BranchMonthlyBudgetDto | null>(null);
  const [rejectionReasons, setRejectionReasons] = useState<Record<string, string>>({});
  const [loadingMembership, setLoadingMembership] = useState(true);
  const [loadingQueue, setLoadingQueue] = useState(false);
  const [busyKey, setBusyKey] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const isManager = membership?.isActive === true
    && membership.role === BusinessRole.Manager
    && Boolean(membership.branchId);
  const isProcurement = membership?.isActive === true
    && membership.role === BusinessRole.Procurement;
  const canAccessQueue = isManager || isProcurement;

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
          setError(getApiErrorMessage(caughtError, { defaultMessage: 'Unable to load current membership.' }));
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
      const response = await purchaseRequestApi.listApprovalQueue(membership.organizationId);
      if (!response.data) {
        throw new Error('Approval queue response missing data.');
      }

      let currentBudget: BranchMonthlyBudgetDto | null = null;
      if (isManager && membership.branchId) {
        const today = new Date();
        try {
          const budgetResponse = await purchaseRequestApi.getBudget(
            membership.organizationId,
            membership.branchId,
            today.getUTCFullYear(),
            today.getUTCMonth() + 1,
          );
          currentBudget = budgetResponse.data;
        } catch (caughtError) {
          if (!isMissingBudget(caughtError)) {
            throw caughtError;
          }
        }
      }

      setQueue(response.data);
      setBudget(currentBudget);
    } catch (caughtError) {
      setError(getApiErrorMessage(caughtError, { defaultMessage: 'Unable to load the approval queue.' }));
    } finally {
      setLoadingQueue(false);
    }
  }, [canAccessQueue, isManager, membership]);

  useEffect(() => {
    void loadQueue();
  }, [loadQueue]);

  const handleDecision = async (
    item: PurchaseRequestApprovalQueueItemDto,
    approve: boolean,
  ) => {
    if (!membership || !item.canDecide) {
      return;
    }

    const rejectionReason = rejectionReasons[item.id]?.trim() || null;
    if (!approve && !rejectionReason) {
      setError('A rejection reason is required.');
      return;
    }

    const operationKey = `${item.id}-${approve ? 'approve' : 'reject'}`;
    setBusyKey(operationKey);
    setError(null);
    setNotice(null);

    try {
      const response = await purchaseRequestApi.decide(
        membership.organizationId,
        item.id,
        {
          concurrencyStamp: item.concurrencyStamp,
          approve,
          rejectionReason: approve ? null : rejectionReason,
        },
      );
      if (!response.data) {
        throw new Error('Decision response missing data.');
      }

      setNotice(approve ? 'Purchase request approved.' : 'Purchase request rejected.');
      setRejectionReasons((current) => {
        const next = { ...current };
        delete next[item.id];
        return next;
      });
      await loadQueue();
    } catch (caughtError) {
      if (isConflict(caughtError)) {
        await loadQueue();
        setError('This request changed in another session. The latest queue has been loaded.');
      } else {
        setError(getApiErrorMessage(caughtError, { defaultMessage: 'Unable to save the decision.' }));
      }
    } finally {
      setBusyKey(null);
    }
  };

  const handleRejectionReasonSubmit = (
    event: FormEvent<HTMLFormElement>,
    item: PurchaseRequestApprovalQueueItemDto,
  ) => {
    event.preventDefault();
    void handleDecision(item, false);
  };

  if (loadingMembership) {
    return (
      <section className="page-shell">
        <div className="page-state">
          <p>Loading approval workspace...</p>
        </div>
      </section>
    );
  }

  if (!canAccessQueue) {
    return (
      <section className="page-shell">
        <div className="page-state">
          <WalletCards aria-hidden="true" size={32} />
          <h1>Purchase request approvals</h1>
          <p>{error ?? 'An active Manager or Procurement membership is required.'}</p>
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
          <h1>{isManager ? 'Branch approval queue' : 'Procurement approval queue'}</h1>
          <p className="page-note">
            {isManager
              ? 'Review submitted requests from your assigned branch.'
              : 'Review requests escalated beyond the available branch budget.'}
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
          {isManager ? (
            <section className="card">
              <div className="purchase-request-section-heading">
                <div>
                  <p className="eyebrow">Current month</p>
                  <h2>Branch budget</h2>
                </div>
                <WalletCards aria-hidden="true" size={22} />
              </div>
              {budget ? (
                <dl className="purchase-request-item__totals">
                  <div>
                    <dt>Available</dt>
                    <dd>{formatMoney(budget.availableAmount)}</dd>
                  </div>
                  <div>
                    <dt>Used</dt>
                    <dd>{formatMoney(budget.usedAmount)}</dd>
                  </div>
                  <div>
                    <dt>Limit</dt>
                    <dd>{formatMoney(budget.limitAmount)}</dd>
                  </div>
                </dl>
              ) : (
                <p className="page-note">No budget is configured for the current month.</p>
              )}
            </section>
          ) : null}

          <section className="card">
            <div className="purchase-request-section-heading">
              <div>
                <p className="eyebrow">Queue</p>
                <h2>Pending requests</h2>
              </div>
              <span className="role-badge">{queue?.items.length ?? 0}</span>
            </div>
            <p className="page-note">
              Decisions are protected by the request version and are refreshed after a conflict.
            </p>
          </section>
        </aside>

        <div className="purchase-request-editor">
          <section className="card">
            <div className="purchase-request-heading">
              <div>
                <p className="eyebrow">{queue?.queueRole === BusinessRole.Manager ? 'Manager' : 'Procurement'}</p>
                <h2>Requests waiting for a decision</h2>
              </div>
            </div>

            {loadingQueue ? <p className="page-note">Loading approval queue...</p> : null}
            {!loadingQueue && queue?.items.length === 0 ? (
              <p className="page-note">No purchase requests are waiting for your decision.</p>
            ) : null}

            <div className="purchase-request-items">
              {queue?.items.map((item) => {
                const approveKey = `${item.id}-approve`;
                const rejectKey = `${item.id}-reject`;
                const canDecide = item.canDecide;

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
                      </dl>
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
                      {canDecide ? (
                        <>
                          <button
                            className="button"
                            type="button"
                            onClick={() => void handleDecision(item, true)}
                            disabled={busyKey !== null}
                          >
                            <Check aria-hidden="true" size={17} />
                            {busyKey === approveKey ? 'Approving...' : 'Approve'}
                          </button>
                          <form className="form" onSubmit={(event) => handleRejectionReasonSubmit(event, item)}>
                            <div className="field">
                              <label className="field__label" htmlFor={`rejection-${item.id}`}>
                                Rejection reason
                              </label>
                              <textarea
                                id={`rejection-${item.id}`}
                                value={rejectionReasons[item.id] ?? ''}
                                onChange={(event) => setRejectionReasons((current) => ({
                                  ...current,
                                  [item.id]: event.target.value,
                                }))}
                                maxLength={1000}
                                rows={3}
                              />
                            </div>
                            <button
                              className="button button--danger"
                              type="submit"
                              disabled={busyKey !== null}
                            >
                              <XCircle aria-hidden="true" size={17} />
                              {busyKey === rejectKey ? 'Rejecting...' : 'Reject'}
                            </button>
                          </form>
                        </>
                      ) : (
                        <p className="page-note">You cannot decide your own request.</p>
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
