import { FormEvent, useEffect, useRef, useState } from 'react';
import { Download, FilePlus2, Paperclip, RefreshCw, Save, ShoppingCart, Trash2 } from 'lucide-react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  BusinessRole,
  PurchaseRequestStatus,
  type CurrentMembershipDto,
  type PurchaseRequestDetailsResponse,
  type PurchaseRequestDto,
  type PurchaseRequestAttachmentDto,
  type PurchaseRequestItemDto,
  type PurchaseRequestListItemDto,
  type SelectableProductDto,
} from '../types';
import { catalogApi, HttpError, organizationApi, purchaseRequestApi } from '../services/api';
import { getApiErrorMessage } from '../utils/helpers';

function formatMoney(value: number): string {
  return `${value.toFixed(2)} PLN`;
}

function formatDate(value: string): string {
  return new Date(value).toLocaleString();
}

function getStatusLabel(status: PurchaseRequestStatus): string {
  return status === PurchaseRequestStatus.Draft
    ? 'Draft'
    : PurchaseRequestStatus[status] ?? 'Unknown';
}

function isConflict(error: unknown): boolean {
  return error instanceof HttpError && error.status === 409;
}

export default function PurchaseRequestDrafts() {
  const navigate = useNavigate();
  const { purchaseRequestId } = useParams<{ purchaseRequestId: string }>();
  const [membership, setMembership] = useState<CurrentMembershipDto | null>(null);
  const [drafts, setDrafts] = useState<PurchaseRequestListItemDto[]>([]);
  const [selectedDraft, setSelectedDraft] = useState<PurchaseRequestDto | null>(null);
  const [products, setProducts] = useState<SelectableProductDto[]>([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [newDraftNote, setNewDraftNote] = useState('');
  const [selectedProductId, setSelectedProductId] = useState('');
  const [quantity, setQuantity] = useState('1');
  const [comment, setComment] = useState('');
  const [quantityValues, setQuantityValues] = useState<Record<string, string>>({});
  const [attachments, setAttachments] = useState<PurchaseRequestAttachmentDto[]>([]);
  const [loadingAttachments, setLoadingAttachments] = useState(false);
  const [loadingMembership, setLoadingMembership] = useState(true);
  const [loadingDrafts, setLoadingDrafts] = useState(false);
  const [loadingProducts, setLoadingProducts] = useState(false);
  const [loadingDetails, setLoadingDetails] = useState(false);
  const [busyKey, setBusyKey] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const canEditDraft = membership?.isActive === true
    && membership.role === BusinessRole.Employee
    && Boolean(membership.branchId);
  const canModifyAttachments = canEditDraft
    && selectedDraft?.status === PurchaseRequestStatus.Draft
    && selectedDraft.authorUserId === membership?.userId;

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

  useEffect(() => {
    if (!membership || !canEditDraft) {
      setLoadingDrafts(false);
      return undefined;
    }

    let active = true;
    setLoadingDrafts(true);
    setError(null);

    void purchaseRequestApi.listDrafts(membership.organizationId, page)
      .then((response) => {
        if (!response.data) {
          throw new Error('Purchase request list response missing data.');
        }

        if (active) {
          setDrafts(response.data.items);
          setTotalCount(response.data.totalCount);
        }
      })
      .catch((caughtError) => {
        if (active) {
          setError(getApiErrorMessage(caughtError, { defaultMessage: 'Unable to load purchase request drafts.' }));
        }
      })
      .finally(() => {
        if (active) {
          setLoadingDrafts(false);
        }
      });

    return () => {
      active = false;
    };
  }, [canEditDraft, membership, page]);

  useEffect(() => {
    if (!membership || !canEditDraft) {
      setLoadingProducts(false);
      return undefined;
    }

    let active = true;
    setLoadingProducts(true);

    void catalogApi.getSelectableProducts(membership.organizationId)
      .then((response) => {
        if (!response.data) {
          throw new Error('Catalog product response missing data.');
        }

        if (active) {
          setProducts(response.data);
        }
      })
      .catch((caughtError) => {
        if (active) {
          setError(getApiErrorMessage(caughtError, { defaultMessage: 'Unable to load catalog products.' }));
        }
      })
      .finally(() => {
        if (active) {
          setLoadingProducts(false);
        }
      });

    return () => {
      active = false;
    };
  }, [canEditDraft, membership]);

  useEffect(() => {
    if (!membership || !purchaseRequestId) {
      setSelectedDraft(null);
      setLoadingDetails(false);
      return undefined;
    }

    let active = true;
    setLoadingDetails(true);
    setError(null);

    void purchaseRequestApi.getDetails(membership.organizationId, purchaseRequestId)
      .then((response) => {
        if (!response.data) {
          throw new Error('Purchase request details response missing data.');
        }

        if (active) {
          setSelectedDraft(response.data);
        }
      })
      .catch((caughtError) => {
        if (active) {
          setSelectedDraft(null);
          setError(getApiErrorMessage(caughtError, { defaultMessage: 'Unable to load the selected draft.' }));
        }
      })
      .finally(() => {
        if (active) {
          setLoadingDetails(false);
        }
      });

    return () => {
      active = false;
    };
  }, [membership, purchaseRequestId]);

  useEffect(() => {
    if (!selectedDraft) {
      setQuantityValues({});
      return;
    }

    setQuantityValues(Object.fromEntries(
      selectedDraft.items.map((item) => [item.id, String(item.quantity)]),
    ));
  }, [selectedDraft]);

  useEffect(() => {
    if (!membership || !selectedDraft) {
      setAttachments([]);
      setLoadingAttachments(false);
      return undefined;
    }

    let active = true;
    setAttachments([]);
    setLoadingAttachments(true);

    void purchaseRequestApi.listAttachments(membership.organizationId, selectedDraft.id)
      .then((response) => {
        if (!response.data) {
          throw new Error('Purchase request attachment response missing data.');
        }

        if (active) {
          setAttachments(response.data);
        }
      })
      .catch((caughtError) => {
        if (active) {
          setError(getApiErrorMessage(caughtError, { defaultMessage: 'Unable to load request attachments.' }));
        }
      })
      .finally(() => {
        if (active) {
          setLoadingAttachments(false);
        }
      });

    return () => {
      active = false;
    };
  }, [membership, selectedDraft?.id]);

  const reloadDrafts = async () => {
    if (!membership) {
      return;
    }

    setLoadingDrafts(true);
    try {
      const response = await purchaseRequestApi.listDrafts(membership.organizationId, page);
      if (!response.data) {
        throw new Error('Purchase request list response missing data.');
      }
      setDrafts(response.data.items);
      setTotalCount(response.data.totalCount);
    } catch (caughtError) {
      setError(getApiErrorMessage(caughtError, { defaultMessage: 'Unable to refresh purchase request drafts.' }));
    } finally {
      setLoadingDrafts(false);
    }
  };

  const reloadSelectedDraft = async () => {
    if (!membership || !selectedDraft) {
      return;
    }

    try {
      const response = await purchaseRequestApi.getDetails(
        membership.organizationId,
        selectedDraft.id,
      );
      if (!response.data) {
        throw new Error('Purchase request details response missing data.');
      }
      setSelectedDraft(response.data);
      await reloadDrafts();
    } catch (caughtError) {
      setError(getApiErrorMessage(caughtError, { defaultMessage: 'Unable to refresh the selected draft.' }));
    }
  };

  const handleCreateDraft = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!membership) {
      return;
    }

    setCreating(true);
    setError(null);
    setNotice(null);

    try {
      const response = await purchaseRequestApi.createDraft(membership.organizationId, {
        note: newDraftNote.trim() || null,
      });
      if (!response.data) {
        throw new Error('Created purchase request response missing data.');
      }

      setNewDraftNote('');
      setNotice('Draft created.');
      await reloadDrafts();
      navigate(`/purchase-requests/${response.data.id}`);
    } catch (caughtError) {
      setError(getApiErrorMessage(caughtError, { defaultMessage: 'Unable to create the draft.' }));
    } finally {
      setCreating(false);
    }
  };

  const runMutation = async (
    busyMutationKey: string,
    successMessage: string,
    operation: (concurrencyStamp: string) => Promise<PurchaseRequestDetailsResponse>,
  ): Promise<boolean> => {
    if (!membership || !selectedDraft) {
      return false;
    }

    setBusyKey(busyMutationKey);
    setError(null);
    setNotice(null);

    try {
      const response = await operation(selectedDraft.concurrencyStamp);
      if (!response.data) {
        throw new Error('Purchase request mutation response missing data.');
      }

      setSelectedDraft(response.data);
      setNotice(successMessage);
      await reloadDrafts();
      return true;
    } catch (caughtError) {
      if (isConflict(caughtError)) {
        setError('This draft changed in another session. The latest version has been loaded.');
        await reloadSelectedDraft();
      } else {
        setError(getApiErrorMessage(caughtError, { defaultMessage: 'Unable to update the draft.' }));
      }
      return false;
    } finally {
      setBusyKey(null);
    }
  };

  const handleAddItem = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!membership || !selectedDraft) {
      return;
    }

    const parsedQuantity = Number(quantity);
    if (!selectedProductId || !Number.isFinite(parsedQuantity) || parsedQuantity <= 0) {
      setError('Select a product and enter a quantity greater than zero.');
      return;
    }

    const succeeded = await runMutation('add-item', 'Item added.', (concurrencyStamp) =>
      purchaseRequestApi.addItem(membership.organizationId, selectedDraft.id, {
        productId: selectedProductId,
        quantity: parsedQuantity,
        comment: comment.trim() || null,
        concurrencyStamp,
      }),
    );

    if (succeeded) {
      setSelectedProductId('');
      setQuantity('1');
      setComment('');
    }
  };

  const handleUpdateQuantity = async (event: FormEvent<HTMLFormElement>, item: PurchaseRequestItemDto) => {
    event.preventDefault();
    const parsedQuantity = Number(quantityValues[item.id]);
    if (!Number.isFinite(parsedQuantity) || parsedQuantity <= 0) {
      setError('Quantity must be greater than zero.');
      return;
    }

    await runMutation(`quantity-${item.id}`, 'Quantity updated.', (concurrencyStamp) =>
      purchaseRequestApi.updateItemQuantity(
        membership!.organizationId,
        selectedDraft!.id,
        item.id,
        { quantity: parsedQuantity, concurrencyStamp },
      ),
    );
  };

  const handleRemoveItem = async (item: PurchaseRequestItemDto) => {
    await runMutation(`remove-${item.id}`, 'Item removed.', (concurrencyStamp) =>
      purchaseRequestApi.removeItem(
        membership!.organizationId,
        selectedDraft!.id,
        item.id,
        { concurrencyStamp },
      ),
    );
  };

  const handleUploadAttachment = async (file: File): Promise<boolean> => {
    if (!membership || !selectedDraft || !canModifyAttachments) {
      return false;
    }

    if (file.size <= 0 || file.size > 10 * 1024 * 1024) {
      setError('Attachments must be greater than zero and 10 MB or smaller.');
      return false;
    }

    setBusyKey('attachment-upload');
    setError(null);
    setNotice(null);

    try {
      const response = await purchaseRequestApi.uploadAttachment(
        membership.organizationId,
        selectedDraft.id,
        file,
      );
      if (!response.data) {
        throw new Error('Uploaded attachment response missing data.');
      }

      setAttachments((current) => [response.data!, ...current]);
      setNotice('Attachment uploaded.');
      return true;
    } catch (caughtError) {
      if (isConflict(caughtError)) {
        setError('This draft changed in another session. The latest version has been loaded.');
        await reloadSelectedDraft();
      } else {
        setError(getApiErrorMessage(caughtError, { defaultMessage: 'Unable to upload the attachment.' }));
      }
      return false;
    } finally {
      setBusyKey(null);
    }
  };

  const handleDownloadAttachment = async (attachment: PurchaseRequestAttachmentDto) => {
    if (!membership || !selectedDraft) {
      return;
    }

    setBusyKey(`attachment-download-${attachment.id}`);
    setError(null);

    try {
      const blob = await purchaseRequestApi.downloadAttachment(
        membership.organizationId,
        selectedDraft.id,
        attachment.id,
      );
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = attachment.originalFileName;
      link.click();
      URL.revokeObjectURL(url);
    } catch (caughtError) {
      setError(getApiErrorMessage(caughtError, { defaultMessage: 'Unable to download the attachment.' }));
    } finally {
      setBusyKey(null);
    }
  };

  const handleDeleteAttachment = async (attachmentId: string) => {
    if (!membership || !selectedDraft || !canModifyAttachments) {
      return;
    }

    setBusyKey(`attachment-delete-${attachmentId}`);
    setError(null);
    setNotice(null);

    try {
      const response = await purchaseRequestApi.deleteAttachment(
        membership.organizationId,
        selectedDraft.id,
        attachmentId,
      );
      if (!response.data) {
        throw new Error('Attachment delete response missing data.');
      }

      setAttachments((current) => current.filter((attachment) => attachment.id !== attachmentId));
      setNotice('Attachment deleted.');
    } catch (caughtError) {
      if (isConflict(caughtError)) {
        setError('This draft changed in another session. The latest version has been loaded.');
        await reloadSelectedDraft();
      } else {
        setError(getApiErrorMessage(caughtError, { defaultMessage: 'Unable to delete the attachment.' }));
      }
    } finally {
      setBusyKey(null);
    }
  };

  const totalPages = Math.max(1, Math.ceil(totalCount / 20));

  if (loadingMembership) {
    return <section className="page-shell"><div className="page-state"><p>Loading purchase request workspace...</p></div></section>;
  }

  if (!canEditDraft) {
    return (
      <section className="page-shell">
        <div className="page-state">
          <ShoppingCart aria-hidden="true" size={32} />
          <h1>Purchase requests</h1>
          <p>{error ?? 'An active Employee branch membership is required.'}</p>
        </div>
      </section>
    );
  }

  return (
    <section className="page-shell purchase-requests-page">
      <header className="page-shell__header">
        <div className="stack stack--tight">
          <p className="eyebrow">Procurement</p>
          <h1>Purchase request drafts</h1>
          <p className="page-note">Build an organization-scoped request from the current catalog.</p>
        </div>
        <button className="button button--ghost" type="button" onClick={() => void reloadDrafts()} disabled={loadingDrafts}>
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
                <p className="eyebrow">New request</p>
                <h2>Start a draft</h2>
              </div>
              <FilePlus2 aria-hidden="true" size={22} />
            </div>
            <form className="form" onSubmit={handleCreateDraft}>
              <div className="field">
                <label className="field__label" htmlFor="new-draft-note">Note</label>
                <textarea id="new-draft-note" value={newDraftNote} onChange={(event) => setNewDraftNote(event.target.value)} maxLength={2000} rows={3} />
              </div>
              <button className="button" type="submit" disabled={creating}>
                <FilePlus2 aria-hidden="true" size={17} />
                {creating ? 'Creating...' : 'Create draft'}
              </button>
            </form>
          </section>

          <section className="card">
            <div className="purchase-request-section-heading">
              <div>
                <p className="eyebrow">Workspace</p>
                <h2>Your drafts</h2>
              </div>
              <span className="role-badge">{totalCount}</span>
            </div>
            {loadingDrafts ? <p className="page-note">Loading drafts...</p> : drafts.length === 0 ? <p className="page-note">No drafts found.</p> : (
              <div className="purchase-request-list">
                {drafts.map((draft) => (
                  <button
                    className={`purchase-request-list__item ${draft.id === selectedDraft?.id ? 'purchase-request-list__item--active' : ''}`}
                    type="button"
                    key={draft.id}
                    onClick={() => navigate(`/purchase-requests/${draft.id}`)}
                  >
                    <span>
                      <strong>{draft.note || `Draft ${draft.id.slice(0, 8)}`}</strong>
                      <small>{draft.itemCount} {draft.itemCount === 1 ? 'item' : 'items'} · {formatMoney(draft.totalValue)}</small>
                    </span>
                    <time dateTime={draft.updatedAt}>{formatDate(draft.updatedAt)}</time>
                  </button>
                ))}
              </div>
            )}
            {totalPages > 1 ? (
              <div className="purchase-request-pagination">
                <button className="button button--ghost" type="button" disabled={page <= 1 || loadingDrafts} onClick={() => setPage((current) => current - 1)}>Previous</button>
                <span className="role-badge">{page} / {totalPages}</span>
                <button className="button button--ghost" type="button" disabled={page >= totalPages || loadingDrafts} onClick={() => setPage((current) => current + 1)}>Next</button>
              </div>
            ) : null}
          </section>
        </aside>

        <main className="purchase-request-editor">
          {loadingDetails ? <div className="page-state"><p>Loading draft...</p></div> : selectedDraft ? (
            <>
              <section className="card purchase-request-heading">
                <div>
                  <p className="eyebrow">Draft details</p>
                  <h2>{selectedDraft.note || `Draft ${selectedDraft.id.slice(0, 8)}`}</h2>
                  <p className="page-note">Updated {formatDate(selectedDraft.updatedAt)}</p>
                </div>
                <span className="role-badge">{getStatusLabel(selectedDraft.status)}</span>
              </section>

              <section className="card">
                <div className="purchase-request-section-heading">
                  <div>
                    <p className="eyebrow">Catalog</p>
                    <h2>Add an item</h2>
                  </div>
                  <ShoppingCart aria-hidden="true" size={22} />
                </div>
                <form className="purchase-request-add-form" onSubmit={handleAddItem}>
                  <div className="field">
                    <label className="field__label" htmlFor="catalog-product">Product</label>
                    <select id="catalog-product" value={selectedProductId} onChange={(event) => setSelectedProductId(event.target.value)} disabled={loadingProducts || products.length === 0}>
                      <option value="">{loadingProducts ? 'Loading products...' : products.length === 0 ? 'No selectable products' : 'Select a product'}</option>
                      {products.map((product) => <option key={product.id} value={product.id}>{product.name}{product.code ? ` (${product.code})` : ''} · {formatMoney(product.unitPrice)} / {product.unitSymbol}</option>)}
                    </select>
                  </div>
                  <div className="field">
                    <label className="field__label" htmlFor="catalog-quantity">Quantity</label>
                    <input id="catalog-quantity" type="number" min="0.01" step="0.01" value={quantity} onChange={(event) => setQuantity(event.target.value)} />
                  </div>
                  <div className="field purchase-request-add-form__comment">
                    <label className="field__label" htmlFor="catalog-comment">Comment</label>
                    <input id="catalog-comment" value={comment} onChange={(event) => setComment(event.target.value)} maxLength={500} />
                  </div>
                  <button className="button" type="submit" disabled={busyKey === 'add-item' || loadingProducts || products.length === 0}>
                    <FilePlus2 aria-hidden="true" size={17} />
                    {busyKey === 'add-item' ? 'Adding...' : 'Add item'}
                  </button>
                </form>
              </section>

              <section className="card">
                <div className="purchase-request-section-heading">
                  <div>
                    <p className="eyebrow">Request lines</p>
                    <h2>Items</h2>
                  </div>
                  <span className="role-badge">{selectedDraft.items.length}</span>
                </div>
                {selectedDraft.items.length === 0 ? <p className="page-note">This draft has no items yet.</p> : (
                  <div className="purchase-request-items">
                    {selectedDraft.items.map((item) => (
                      <article className="purchase-request-item" key={item.id}>
                        <div className="purchase-request-item__details">
                          <div>
                            <h3>{item.productName}</h3>
                            <p>{item.productCode || 'Catalog product'} · {formatMoney(item.unitPrice)} / {item.unitSymbol}</p>
                          </div>
                          {item.comment ? <p className="page-note">{item.comment}</p> : null}
                          <dl className="purchase-request-item__totals">
                            <div><dt>Line total</dt><dd>{formatMoney(item.lineTotal)}</dd></div>
                            <div><dt>Unit</dt><dd>{item.unitName}</dd></div>
                          </dl>
                        </div>
                        <div className="purchase-request-item__actions">
                          <form className="purchase-request-quantity-form" onSubmit={(event) => void handleUpdateQuantity(event, item)}>
                            <label className="field__label" htmlFor={`quantity-${item.id}`}>Quantity</label>
                            <input id={`quantity-${item.id}`} type="number" min="0.01" step="0.01" value={quantityValues[item.id] ?? String(item.quantity)} onChange={(event) => setQuantityValues((current) => ({ ...current, [item.id]: event.target.value }))} />
                            <button className="button button--ghost" type="submit" disabled={busyKey === `quantity-${item.id}`}>
                              <Save aria-hidden="true" size={16} />
                              {busyKey === `quantity-${item.id}` ? 'Saving...' : 'Save quantity'}
                            </button>
                          </form>
                          <button className="button button--danger" type="button" disabled={busyKey === `remove-${item.id}`} onClick={() => void handleRemoveItem(item)}>
                            <Trash2 aria-hidden="true" size={16} />
                            {busyKey === `remove-${item.id}` ? 'Removing...' : 'Remove'}
                          </button>
                        </div>
                      </article>
                    ))}
                  </div>
                )}
              </section>

              <section className="card purchase-request-attachments" aria-label="Purchase request attachments">
                <div className="purchase-request-section-heading">
                  <div>
                    <p className="eyebrow">Supporting files</p>
                    <h2>Attachments</h2>
                  </div>
                  <Paperclip aria-hidden="true" size={22} />
                </div>
                {loadingAttachments ? <p className="page-note">Loading attachments...</p> : attachments.length === 0 ? <p className="page-note">No attachments yet.</p> : (
                  <div className="purchase-request-attachments__list">
                    {attachments.map((attachment) => (
                      <article className="purchase-request-attachments__item" key={attachment.id}>
                        <div>
                          <strong>{attachment.originalFileName}</strong>
                          <small>{formatDate(attachment.createdAt)} · {formatAttachmentSize(attachment.sizeBytes)}</small>
                        </div>
                        <div className="purchase-request-attachments__actions">
                          <button className="button button--ghost" type="button" disabled={busyKey === `attachment-download-${attachment.id}`} onClick={() => void handleDownloadAttachment(attachment)}>
                            <Download aria-hidden="true" size={16} />
                            {busyKey === `attachment-download-${attachment.id}` ? 'Downloading...' : 'Download'}
                          </button>
                          {canModifyAttachments && attachment.uploadedByUserId === membership?.userId ? (
                            <button className="button button--danger" type="button" disabled={busyKey === `attachment-delete-${attachment.id}`} onClick={() => void handleDeleteAttachment(attachment.id)}>
                              <Trash2 aria-hidden="true" size={16} />
                              {busyKey === `attachment-delete-${attachment.id}` ? 'Deleting...' : 'Delete'}
                            </button>
                          ) : null}
                        </div>
                      </article>
                    ))}
                  </div>
                )}
                {canModifyAttachments ? (
                  <AttachmentUploadForm
                    busy={busyKey === 'attachment-upload'}
                    onUpload={handleUploadAttachment}
                  />
                ) : <p className="page-note">Attachments can be added or removed only while this request is your draft.</p>}
              </section>

              <section className="purchase-request-total" aria-label="Purchase request total">
                <span>Total value</span>
                <strong>{formatMoney(selectedDraft.totalValue)}</strong>
              </section>
            </>
          ) : (
            <div className="page-state">
              <ShoppingCart aria-hidden="true" size={32} />
              <h2>Select a draft</h2>
              <p>Create a new draft or choose one from the list.</p>
            </div>
          )}
        </main>
      </div>
    </section>
  );
}

function formatAttachmentSize(sizeBytes: number): string {
  if (sizeBytes < 1024) return `${sizeBytes} B`;
  if (sizeBytes < 1024 * 1024) return `${(sizeBytes / 1024).toFixed(1)} KB`;
  return `${(sizeBytes / (1024 * 1024)).toFixed(1)} MB`;
}

function AttachmentUploadForm({
  busy,
  onUpload,
}: {
  busy: boolean;
  onUpload: (file: File) => Promise<boolean>;
}) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!selectedFile) {
      return;
    }

    const uploaded = await onUpload(selectedFile);
    if (!uploaded) {
      return;
    }

    setSelectedFile(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  return (
    <form className="purchase-request-attachments__form" onSubmit={(event) => void submit(event)}>
      <label className="field__label" htmlFor="purchase-request-attachment">Choose a file</label>
      <input
        ref={fileInputRef}
        id="purchase-request-attachment"
        type="file"
        accept=".pdf,.png,.jpg,.jpeg,.docx,.xlsx,.txt"
        onChange={(event) => setSelectedFile(event.target.files?.[0] ?? null)}
      />
      <button className="button" type="submit" disabled={!selectedFile || busy}>
        <Paperclip aria-hidden="true" size={17} />
        {busy ? 'Uploading...' : 'Upload attachment'}
      </button>
    </form>
  );
}
