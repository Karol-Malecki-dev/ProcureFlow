import { Archive, Building2, Edit3, Plus, RefreshCw, UserRound, X } from 'lucide-react';
import { FormEvent, useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { adminApi, organizationApi } from '../services/api';
import type { AdminUserListItemDto } from '../types/admin';
import {
  BusinessRole,
  type ActiveOrganizationDto,
  type BranchDto,
  type CreateMembershipRequest,
  type MembershipListItemDto,
  type UpdateMembershipRequest,
} from '../types/organization';
import { getApiErrorMessage } from '../utils/helpers';

interface MembershipFormValues {
  userId: string;
  role: BusinessRole;
  branchId: string;
}

const emptyForm: MembershipFormValues = {
  userId: '',
  role: BusinessRole.Employee,
  branchId: '',
};

const businessRoles = [
  { value: BusinessRole.Employee, label: 'Employee' },
  { value: BusinessRole.Manager, label: 'Manager' },
  { value: BusinessRole.Procurement, label: 'Procurement' },
];

function getRoleLabel(role: BusinessRole): string {
  return businessRoles.find((option) => option.value === Number(role))?.label ?? 'Unknown role';
}

function requiresBranch(role: BusinessRole): boolean {
  return role === BusinessRole.Employee || role === BusinessRole.Manager;
}

function getOrganizationLabel(organization: ActiveOrganizationDto | null, organizationId: string | undefined): string {
  return organization ? `${organization.name} (${organization.code})` : `Organization ${organizationId ?? 'unavailable'}`;
}

function getUserLabel(user: AdminUserListItemDto): string {
  return `${user.displayName} - ${user.email}`;
}

export default function OrganizationMemberships() {
  const { organizationId: routeOrganizationId } = useParams<{ organizationId: string }>();
  const [activeOrganization, setActiveOrganization] = useState<ActiveOrganizationDto | null>(null);
  const [branches, setBranches] = useState<BranchDto[]>([]);
  const [users, setUsers] = useState<AdminUserListItemDto[]>([]);
  const [memberships, setMemberships] = useState<MembershipListItemDto[]>([]);
  const [includeInactive, setIncludeInactive] = useState(false);
  const [roleFilter, setRoleFilter] = useState<'all' | BusinessRole>('all');
  const [branchFilter, setBranchFilter] = useState('');
  const [formValues, setFormValues] = useState<MembershipFormValues>(emptyForm);
  const [editingMembership, setEditingMembership] = useState<MembershipListItemDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [referenceLoading, setReferenceLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [busyMembershipId, setBusyMembershipId] = useState<string | null>(null);
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
    if (!organizationId) {
      if (routeOrganizationId) {
        setReferenceLoading(false);
        setLoading(false);
        setError('Organization id is missing from the route.');
      }
      return;
    }

    let active = true;
    setReferenceLoading(true);
    setError(null);

    void Promise.all([
      organizationApi.getBranches(organizationId),
      adminApi.getUsers({ pageNumber: 1, pageSize: 100, isActive: true }),
    ])
      .then(([branchResponse, userResponse]) => {
        if (active) {
          setBranches(branchResponse.data ?? []);
          setUsers(userResponse.data ?? []);
        }
      })
      .catch((caughtError) => {
        if (active) {
          setError(getApiErrorMessage(caughtError, { defaultMessage: 'Failed to load membership references' }));
        }
      })
      .finally(() => {
        if (active) {
          setReferenceLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, [organizationId, routeOrganizationId]);

  useEffect(() => {
    if (!organizationId) {
      return;
    }

    let active = true;
    setLoading(true);
    setError(null);

    void organizationApi.getMemberships(organizationId, {
      branchId: branchFilter || undefined,
      role: roleFilter === 'all' ? undefined : roleFilter,
      includeInactive,
    })
      .then((response) => {
        if (active) {
          setMemberships(response.data ?? []);
        }
      })
      .catch((caughtError) => {
        if (active) {
          setError(getApiErrorMessage(caughtError, { defaultMessage: 'Failed to load memberships' }));
        }
      })
      .finally(() => {
        if (active) {
          setLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, [branchFilter, includeInactive, organizationId, roleFilter]);

  const reloadMemberships = async () => {
    if (!organizationId) {
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const response = await organizationApi.getMemberships(organizationId, {
        branchId: branchFilter || undefined,
        role: roleFilter === 'all' ? undefined : roleFilter,
        includeInactive,
      });
      setMemberships(response.data ?? []);
    } catch (caughtError) {
      setError(getApiErrorMessage(caughtError, { defaultMessage: 'Failed to load memberships' }));
    } finally {
      setLoading(false);
    }
  };

  const handleFormChange = (field: keyof MembershipFormValues, value: string) => {
    setFormValues((current) => ({ ...current, [field]: value }));
  };

  const handleRoleChange = (value: string) => {
    const role = Number(value) as BusinessRole;
    setFormValues((current) => ({
      ...current,
      role,
      branchId: requiresBranch(role) ? current.branchId : '',
    }));
  };

  const handleEdit = (membership: MembershipListItemDto) => {
    setEditingMembership(membership);
    setFormValues({
      userId: membership.userId,
      role: membership.role,
      branchId: membership.branchId ?? '',
    });
    setActionMessage(null);
    setError(null);
  };

  const handleCancelEdit = () => {
    setEditingMembership(null);
    setFormValues(emptyForm);
    setActionMessage(null);
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!organizationId) {
      setError('Organization id is missing from the route.');
      return;
    }

    if (!editingMembership && !formValues.userId) {
      setError('Select a user before assigning membership.');
      return;
    }

    if (requiresBranch(formValues.role) && !formValues.branchId) {
      setError('Select a branch for this business role.');
      return;
    }

    setSaving(true);
    setError(null);
    setActionMessage(null);
    const branchId = requiresBranch(formValues.role) ? formValues.branchId : null;

    try {
      if (editingMembership) {
        const request: UpdateMembershipRequest = {
          role: formValues.role,
          branchId,
        };
        await organizationApi.updateMembership(organizationId, editingMembership.id, request);
        setActionMessage('Membership updated.');
      } else {
        const request: CreateMembershipRequest = {
          userId: formValues.userId,
          role: formValues.role,
          branchId,
        };
        await organizationApi.createMembership(organizationId, request);
        setActionMessage('Membership assigned.');
      }

      setEditingMembership(null);
      setFormValues(emptyForm);
      await reloadMemberships();
    } catch (caughtError) {
      setError(getApiErrorMessage(caughtError, { defaultMessage: 'Failed to save membership' }));
    } finally {
      setSaving(false);
    }
  };

  const handleDeactivate = async (membership: MembershipListItemDto) => {
    if (!organizationId) {
      return;
    }

    setBusyMembershipId(membership.id);
    setError(null);
    setActionMessage(null);

    try {
      await organizationApi.archiveMembership(organizationId, membership.id);
      if (editingMembership?.id === membership.id) {
        handleCancelEdit();
      }
      setActionMessage('Membership deactivated.');
      await reloadMemberships();
    } catch (caughtError) {
      setError(getApiErrorMessage(caughtError, { defaultMessage: 'Failed to deactivate membership' }));
    } finally {
      setBusyMembershipId(null);
    }
  };

  const activeUsers = users.filter((user) => user.isActive);

  return (
    <section className="page-shell">
      <div className="page-shell__header">
        <div className="stack stack--tight">
          <p className="eyebrow">Admin only</p>
          <h1>Organization access</h1>
          <p className="page-note">Assign one active business membership per user and keep deactivated assignments available for history.</p>
        </div>
        <div className="hero__actions">
          <Link className="button button--ghost" to="/admin">
            Back to admin panel
          </Link>
          <button className="button button--ghost" type="button" onClick={() => void reloadMemberships()} disabled={loading}>
            <RefreshCw aria-hidden="true" size={17} />
            Refresh
          </button>
        </div>
      </div>

      <div className="toolbar">
        <div className="toolbar__group">
          <Building2 aria-hidden="true" size={20} />
          <span className="page-note">{getOrganizationLabel(activeOrganization, organizationId)}</span>
        </div>
        <div className="toolbar__group">
          <label className="toggle-field">
            <input
              type="checkbox"
              checked={includeInactive}
              onChange={(event) => setIncludeInactive(event.target.checked)}
            />
            Include inactive
          </label>
        </div>
      </div>

      <div className="toolbar">
        <div className="toolbar__group">
          <label className="field">
            <span className="field__label">Role</span>
            <select
              className="toolbar__select"
              aria-label="Filter by role"
              value={roleFilter}
              onChange={(event) => setRoleFilter(event.target.value === 'all' ? 'all' : Number(event.target.value) as BusinessRole)}
            >
              <option value="all">All roles</option>
              {businessRoles.map((role) => <option value={role.value} key={role.value}>{role.label}</option>)}
            </select>
          </label>
          <label className="field">
            <span className="field__label">Branch</span>
            <select
              className="toolbar__select"
              aria-label="Filter by branch"
              value={branchFilter}
              onChange={(event) => setBranchFilter(event.target.value)}
            >
              <option value="">All branches</option>
              {branches.map((branch) => <option value={branch.id} key={branch.id}>{branch.name}</option>)}
            </select>
          </label>
        </div>
        <span className="page-note">{memberships.length} membership{memberships.length === 1 ? '' : 's'} shown</span>
      </div>

      {referenceLoading || loading ? <div className="page-state" role="status">Loading organization access...</div> : null}
      {error ? <p className="form__error" role="alert">{error}</p> : null}
      {actionMessage ? <p className="form__success" role="status">{actionMessage}</p> : null}

      <div className="grid grid--2">
        <article className="card stack">
          <div className="stack stack--tight">
            <p className="eyebrow">{editingMembership ? 'Change assignment' : 'Assign membership'}</p>
            <h2>{editingMembership ? editingMembership.userDisplayName : 'Add a user'}</h2>
            <p className="page-note">Employee and Manager assignments require an active branch. Procurement is organization-wide.</p>
          </div>

          <form className="form" onSubmit={handleSubmit}>
            {!editingMembership ? (
              <label className="field" htmlFor="membership-user">
                <span className="field__label">User</span>
                <select
                  id="membership-user"
                  value={formValues.userId}
                  required
                  onChange={(event) => handleFormChange('userId', event.target.value)}
                >
                  <option value="">Select an active user</option>
                  {activeUsers.map((user) => <option value={user.id} key={user.id}>{getUserLabel(user)}</option>)}
                </select>
              </label>
            ) : (
              <div className="toolbar__group">
                <UserRound aria-hidden="true" size={18} />
                <span className="page-note">{editingMembership.userEmail}</span>
              </div>
            )}

            <label className="field" htmlFor="membership-role">
              <span className="field__label">Business role</span>
              <select
                id="membership-role"
                value={formValues.role}
                onChange={(event) => handleRoleChange(event.target.value)}
              >
                {businessRoles.map((role) => <option value={role.value} key={role.value}>{role.label}</option>)}
              </select>
            </label>

            {requiresBranch(formValues.role) ? (
              <label className="field" htmlFor="membership-branch">
                <span className="field__label">Branch</span>
                <select
                  id="membership-branch"
                  value={formValues.branchId}
                  required
                  onChange={(event) => handleFormChange('branchId', event.target.value)}
                >
                  <option value="">Select an active branch</option>
                  {branches.map((branch) => <option value={branch.id} key={branch.id}>{branch.name} ({branch.code})</option>)}
                </select>
              </label>
            ) : (
              <p className="form__warning">Procurement access applies to the organization and does not use a branch.</p>
            )}

            <div className="hero__actions">
              <button className="button" type="submit" disabled={saving || referenceLoading}>
                {editingMembership ? <Edit3 aria-hidden="true" size={17} /> : <Plus aria-hidden="true" size={17} />}
                {saving ? 'Saving...' : editingMembership ? 'Save assignment' : 'Assign membership'}
              </button>
              {editingMembership ? (
                <button className="button button--ghost" type="button" onClick={handleCancelEdit} disabled={saving}>
                  <X aria-hidden="true" size={17} />
                  Cancel edit
                </button>
              ) : null}
            </div>
          </form>
        </article>

        <div className="stack">
          {!loading && memberships.length === 0 ? (
            <div className="page-state stack stack--tight">
              <UserRound aria-hidden="true" size={28} />
              <h2>No memberships found</h2>
              <p className="page-note">Assign an active user to give them organization access.</p>
            </div>
          ) : null}

          {memberships.map((membership) => {
            const inactive = !membership.isActive;
            const busy = busyMembershipId === membership.id;

            return (
              <article className="card stack stack--tight" key={membership.id}>
                <div className="branch-card__header">
                  <div className="stack stack--tight">
                    <h2>{membership.userDisplayName}</h2>
                    <p className="page-note">{membership.userEmail}</p>
                  </div>
                  <span className={`role-badge ${inactive ? '' : 'role-badge--admin'}`}>
                    {inactive ? 'Inactive' : 'Active'}
                  </span>
                </div>
                <div className="toolbar__group">
                  <span className="role-badge">{getRoleLabel(membership.role)}</span>
                  <span className="page-note">{membership.branchName ?? 'Organization-wide'}</span>
                </div>
                <div className="hero__actions">
                  <button className="button button--ghost" type="button" onClick={() => handleEdit(membership)} disabled={inactive || busy}>
                    <Edit3 aria-hidden="true" size={17} />
                    Edit
                  </button>
                  {!inactive ? (
                    <button className="button button--danger" type="button" onClick={() => void handleDeactivate(membership)} disabled={busy}>
                      <Archive aria-hidden="true" size={17} />
                      {busy ? 'Deactivating...' : 'Deactivate'}
                    </button>
                  ) : null}
                </div>
              </article>
            );
          })}
        </div>
      </div>
    </section>
  );
}