import { Archive, Building2, Edit3, MapPin, Plus, RefreshCw, X } from 'lucide-react';
import { FormEvent, useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { organizationApi } from '../services/api';
import type {
  ActiveOrganizationDto,
  BranchDto,
  CreateBranchRequest,
  OrganizationAddressDto,
  UpdateBranchRequest,
} from '../types/organization';
import { getApiErrorMessage } from '../utils/helpers';

interface BranchFormValues {
  name: string;
  code: string;
  street: string;
  buildingNumber: string;
  apartmentNumber: string;
  city: string;
  postalCode: string;
  country: string;
}

const emptyForm: BranchFormValues = {
  name: '',
  code: '',
  street: '',
  buildingNumber: '',
  apartmentNumber: '',
  city: '',
  postalCode: '',
  country: '',
};

function formFromBranch(branch: BranchDto): BranchFormValues {
  return {
    name: branch.name,
    code: branch.code,
    street: branch.address.street,
    buildingNumber: branch.address.buildingNumber,
    apartmentNumber: branch.address.apartmentNumber ?? '',
    city: branch.address.city,
    postalCode: branch.address.postalCode,
    country: branch.address.country,
  };
}

function addressFromForm(form: BranchFormValues): OrganizationAddressDto {
  return {
    street: form.street.trim(),
    buildingNumber: form.buildingNumber.trim(),
    apartmentNumber: form.apartmentNumber.trim() || null,
    city: form.city.trim(),
    postalCode: form.postalCode.trim(),
    country: form.country.trim(),
  };
}

function requestFromForm(form: BranchFormValues): CreateBranchRequest {
  return {
    name: form.name.trim(),
    code: form.code.trim(),
    address: addressFromForm(form),
  };
}

function Field({
  id,
  label,
  value,
  onChange,
  required = true,
  type = 'text',
}: {
  id: keyof BranchFormValues;
  label: string;
  value: string;
  onChange: (value: string) => void;
  required?: boolean;
  type?: string;
}) {
  return (
    <label className="field" htmlFor={id}>
      <span className="field__label">{label}</span>
      <input
        id={id}
        name={id}
        type={type}
        value={value}
        required={required}
        onChange={(event) => onChange(event.target.value)}
      />
    </label>
  );
}

function BranchForm({
  values,
  editing,
  saving,
  onChange,
  onSubmit,
  onCancel,
}: {
  values: BranchFormValues;
  editing: boolean;
  saving: boolean;
  onChange: (field: keyof BranchFormValues, value: string) => void;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
  onCancel: () => void;
}) {
  return (
    <form className="form" onSubmit={onSubmit}>
      <div className="grid grid--2">
        <Field id="name" label="Branch name" value={values.name} onChange={(value) => onChange('name', value)} />
        <Field id="code" label="Branch code" value={values.code} onChange={(value) => onChange('code', value)} />
        <Field id="street" label="Street" value={values.street} onChange={(value) => onChange('street', value)} />
        <Field id="buildingNumber" label="Building number" value={values.buildingNumber} onChange={(value) => onChange('buildingNumber', value)} />
        <Field id="apartmentNumber" label="Apartment number" required={false} value={values.apartmentNumber} onChange={(value) => onChange('apartmentNumber', value)} />
        <Field id="city" label="City" value={values.city} onChange={(value) => onChange('city', value)} />
        <Field id="postalCode" label="Postal code" value={values.postalCode} onChange={(value) => onChange('postalCode', value)} />
        <Field id="country" label="Country" value={values.country} onChange={(value) => onChange('country', value)} />
      </div>

      <div className="hero__actions">
        <button className="button" type="submit" disabled={saving}>
          {editing ? <Edit3 aria-hidden="true" size={17} /> : <Plus aria-hidden="true" size={17} />}
          {saving ? 'Saving...' : editing ? 'Save changes' : 'Create branch'}
        </button>
        {editing ? (
          <button className="button button--ghost" type="button" onClick={onCancel} disabled={saving}>
            <X aria-hidden="true" size={17} />
            Cancel edit
          </button>
        ) : null}
      </div>
    </form>
  );
}

function formatAddress(address: OrganizationAddressDto): string {
  const street = [address.street, address.buildingNumber].filter(Boolean).join(' ');
  const locality = [address.postalCode, address.city].filter(Boolean).join(' ');
  return [street, address.apartmentNumber ? `Apartment ${address.apartmentNumber}` : '', locality, address.country]
    .filter(Boolean)
    .join(', ');
}

export default function OrganizationBranches() {
  const { organizationId: routeOrganizationId } = useParams<{ organizationId: string }>();
  const [activeOrganization, setActiveOrganization] = useState<ActiveOrganizationDto | null>(null);
  const [branches, setBranches] = useState<BranchDto[]>([]);
  const [includeArchived, setIncludeArchived] = useState(false);
  const [formValues, setFormValues] = useState<BranchFormValues>(emptyForm);
  const [editingBranch, setEditingBranch] = useState<BranchDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [busyBranchId, setBusyBranchId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [actionMessage, setActionMessage] = useState<string | null>(null);
  const organizationId = routeOrganizationId ?? activeOrganization?.id;

  useEffect(() => {
    if (routeOrganizationId) {
      setActiveOrganization(null);
      return;
    }

    let active = true;
    setActiveOrganization(null);
    setLoading(true);
    setError(null);

    void organizationApi.getActiveOrganization()
      .then((response) => {
        if (active) {
          setActiveOrganization(response.data);
        }
      })
      .catch((caughtError) => {
        if (active) {
          setError(getApiErrorMessage(caughtError, { defaultMessage: 'Failed to load active organization' }));
          setLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, [routeOrganizationId]);

  useEffect(() => {
    let active = true;

    const loadBranches = async () => {
      if (!organizationId) {
        if (routeOrganizationId) {
          setLoading(false);
          setError('Organization id is missing from the route.');
        }
        return;
      }

      setLoading(true);
      setError(null);

      try {
        const response = await organizationApi.getBranches(organizationId, includeArchived);
        if (active) {
          setBranches(response.data ?? []);
        }
      } catch (caughtError) {
        if (active) {
          setError(getApiErrorMessage(caughtError, { defaultMessage: 'Failed to load branches' }));
        }
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    };

    void loadBranches();

    return () => {
      active = false;
    };
  }, [includeArchived, organizationId, routeOrganizationId]);

  const reloadBranches = async () => {
    if (!organizationId) {
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const response = await organizationApi.getBranches(organizationId, includeArchived);
      setBranches(response.data ?? []);
    } catch (caughtError) {
      setError(getApiErrorMessage(caughtError, { defaultMessage: 'Failed to load branches' }));
    } finally {
      setLoading(false);
    }
  };

  const handleFormChange = (field: keyof BranchFormValues, value: string) => {
    setFormValues((current) => ({ ...current, [field]: value }));
  };

  const handleEdit = (branch: BranchDto) => {
    setEditingBranch(branch);
    setFormValues(formFromBranch(branch));
    setActionMessage(null);
    setError(null);
  };

  const handleCancelEdit = () => {
    setEditingBranch(null);
    setFormValues(emptyForm);
    setActionMessage(null);
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!organizationId) {
      setError('Organization id is missing from the route.');
      return;
    }

    setSaving(true);
    setError(null);
    setActionMessage(null);
    const request = requestFromForm(formValues);

    try {
      if (editingBranch) {
        const updateRequest: UpdateBranchRequest = request;
        await organizationApi.updateBranch(organizationId, editingBranch.id, updateRequest);
        setActionMessage('Branch updated.');
      } else {
        await organizationApi.createBranch(organizationId, request);
        setActionMessage('Branch created.');
      }

      setEditingBranch(null);
      setFormValues(emptyForm);
      await reloadBranches();
    } catch (caughtError) {
      setError(getApiErrorMessage(caughtError, { defaultMessage: 'Failed to save branch' }));
    } finally {
      setSaving(false);
    }
  };

  const handleArchive = async (branch: BranchDto) => {
    if (!organizationId) {
      return;
    }

    setBusyBranchId(branch.id);
    setError(null);
    setActionMessage(null);

    try {
      await organizationApi.archiveBranch(organizationId, branch.id);
      if (editingBranch?.id === branch.id) {
        handleCancelEdit();
      }
      setActionMessage('Branch archived.');
      await reloadBranches();
    } catch (caughtError) {
      setError(getApiErrorMessage(caughtError, { defaultMessage: 'Failed to archive branch' }));
    } finally {
      setBusyBranchId(null);
    }
  };

  return (
    <section className="page-shell">
      <div className="page-shell__header">
        <div className="stack stack--tight">
          <p className="eyebrow">Admin only</p>
          <h1>Organization branches</h1>
          <p className="page-note">Manage the active organization&apos;s branch directory. Archived branches remain available for history.</p>
        </div>
        <div className="hero__actions">
          <Link className="button button--ghost" to="/admin">
            Back to admin panel
          </Link>
          <button className="button button--ghost" type="button" onClick={() => void reloadBranches()} disabled={loading}>
            <RefreshCw aria-hidden="true" size={17} />
            Refresh
          </button>
        </div>
      </div>

      <div className="toolbar">
        <div className="toolbar__group">
          <Building2 aria-hidden="true" size={20} />
          <span className="page-note">
            {activeOrganization
              ? `${activeOrganization.name} (${activeOrganization.code})`
              : `Organization ${organizationId ?? 'unavailable'}`}
          </span>
        </div>
        <label className="toggle-field">
          <input
            type="checkbox"
            checked={includeArchived}
            onChange={(event) => setIncludeArchived(event.target.checked)}
          />
          Include archived branches
        </label>
      </div>

      <div className="grid grid--2">
        <article className="card stack">
          <div className="stack stack--tight">
            <p className="eyebrow">{editingBranch ? 'Edit branch' : 'New branch'}</p>
            <h2>{editingBranch ? editingBranch.name : 'Add a branch'}</h2>
            <p className="page-note">Branch names and codes must be unique inside this organization.</p>
          </div>
          <BranchForm
            values={formValues}
            editing={editingBranch !== null}
            saving={saving}
            onChange={handleFormChange}
            onSubmit={handleSubmit}
            onCancel={handleCancelEdit}
          />
        </article>

        <div className="stack">
          {loading ? <div className="page-state" role="status">Loading branches...</div> : null}
          {error ? <p className="form__error" role="alert">{error}</p> : null}
          {actionMessage ? <p className="form__success" role="status">{actionMessage}</p> : null}
          {!loading && !error && branches.length === 0 ? (
            <div className="page-state stack stack--tight">
              <MapPin aria-hidden="true" size={28} />
              <h2>No branches found</h2>
              <p className="page-note">Create the first branch for this organization.</p>
            </div>
          ) : null}

          {!loading && branches.length > 0 ? (
            <div className="branch-list">
              {branches.map((branch) => {
                const archived = branch.isArchived;
                const busy = busyBranchId === branch.id;

                return (
                  <article className="card branch-card stack stack--tight" key={branch.id}>
                    <div className="branch-card__header">
                      <div className="stack stack--tight">
                        <h2>{branch.name}</h2>
                        <p className="page-note">Code: {branch.code}</p>
                      </div>
                      <span className={`role-badge ${archived ? '' : 'role-badge--admin'}`}>
                        {archived ? 'Archived' : 'Active'}
                      </span>
                    </div>
                    <p className="branch-card__address">
                      <MapPin aria-hidden="true" size={16} />
                      {formatAddress(branch.address)}
                    </p>
                    <div className="hero__actions">
                      <button className="button button--ghost" type="button" onClick={() => handleEdit(branch)} disabled={archived || busy}>
                        <Edit3 aria-hidden="true" size={17} />
                        Edit
                      </button>
                      {!archived ? (
                        <button className="button button--danger" type="button" onClick={() => void handleArchive(branch)} disabled={busy}>
                          <Archive aria-hidden="true" size={17} />
                          {busy ? 'Archiving...' : 'Archive'}
                        </button>
                      ) : null}
                    </div>
                  </article>
                );
              })}
            </div>
          ) : null}
        </div>
      </div>
    </section>
  );
}
