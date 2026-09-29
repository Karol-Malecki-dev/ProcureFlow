import { Building2, ClipboardList, Package, RefreshCw, TrendingUp } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { useFeatureAvailability } from '../hooks/useFeatureAvailability';
import { organizationApi, purchaseRequestApi } from '../services/api';
import { BusinessRole, type PurchaseRequestDashboardDto } from '../types';
import { getApiErrorMessage } from '../utils/helpers';

function formatMoney(value: number): string {
  return value.toLocaleString('pl-PL', { style: 'currency', currency: 'PLN' });
}

function formatQuantity(value: number): string {
  return value.toLocaleString('pl-PL', { maximumFractionDigits: 2 });
}

function getRoleLabel(role: BusinessRole): string {
  switch (role) {
    case BusinessRole.Employee:
      return 'Employee';
    case BusinessRole.Manager:
      return 'Manager';
    case BusinessRole.Procurement:
      return 'Procurement';
    default:
      return 'Organization';
  }
}

function PurchaseRequestDashboardOverview() {
  const [dashboard, setDashboard] = useState<PurchaseRequestDashboardDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadDashboard = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const membershipResponse = await organizationApi.getCurrentMembership();
      if (!membershipResponse.data) {
        throw new Error('Current membership response missing data.');
      }

      const dashboardResponse = await purchaseRequestApi.getDashboard(membershipResponse.data.organizationId);
      if (!dashboardResponse.data) {
        throw new Error('Purchase request dashboard response missing data.');
      }

      setDashboard(dashboardResponse.data);
    } catch (caughtError) {
      setError(getApiErrorMessage(caughtError, { defaultMessage: 'Unable to load the purchase request dashboard.' }));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadDashboard();
  }, [loadDashboard]);

  return (
    <section className="dashboard" aria-labelledby="purchase-request-dashboard-heading">
      <header className="dashboard__header page-shell__header">
        <div>
          <p className="eyebrow">ProcureFlow</p>
          <h2 id="purchase-request-dashboard-heading">Purchase request overview</h2>
          {dashboard ? <p className="page-note">Scope: {getRoleLabel(dashboard.scopeRole)}</p> : null}
        </div>
        <button
          className="button button--ghost"
          type="button"
          onClick={() => void loadDashboard()}
          disabled={loading}
        >
          <RefreshCw aria-hidden="true" size={17} />
          Refresh
        </button>
      </header>

      {loading ? <div className="page-state" role="status">Loading purchase request dashboard...</div> : null}
      {error ? (
        <div className="stack stack--tight" role="alert">
          <p className="form__error">{error}</p>
          <button className="button button--ghost" type="button" onClick={() => void loadDashboard()}>
            <RefreshCw aria-hidden="true" size={17} />
            Try again
          </button>
        </div>
      ) : null}

      {!loading && !error && dashboard ? (
        <>
          <div className="dashboard__metrics" aria-label="Purchase request metrics">
            <div>
              <span><ClipboardList aria-hidden="true" size={15} /> Pending requests</span>
              <strong>{dashboard.pendingRequestsCount}</strong>
            </div>
            <div>
              <span><TrendingUp aria-hidden="true" size={15} /> Current month orders</span>
              <strong>{formatMoney(dashboard.currentMonthOrderValue)}</strong>
            </div>
            <div>
              <span><Package aria-hidden="true" size={15} /> Popular products</span>
              <strong>{dashboard.mostFrequentlyOrderedProducts.length}</strong>
            </div>
            <div>
              <span><Building2 aria-hidden="true" size={15} /> Branches with orders</span>
              <strong>{dashboard.spendingByBranch.length}</strong>
            </div>
          </div>

          <div className="dashboard__content">
            <div>
              <h3>Most frequently ordered products</h3>
              {dashboard.mostFrequentlyOrderedProducts.length === 0 ? (
                <p className="page-note">No ordered products for the current month.</p>
              ) : (
                <ul className="dashboard__task-list">
                  {dashboard.mostFrequentlyOrderedProducts.map((product) => (
                    <li key={product.productId}>
                      <div>
                        <strong>{product.productName}</strong>
                        <small>{product.productCode ?? 'No catalog code'} · {product.requestCount} requests</small>
                      </div>
                      <span>{formatQuantity(product.totalQuantity)}</span>
                    </li>
                  ))}
                </ul>
              )}
            </div>

            <div>
              <h3>Spending by branch</h3>
              {dashboard.spendingByBranch.length === 0 ? (
                <p className="page-note">No branch orders for the current month.</p>
              ) : (
                <ul className="dashboard__task-list">
                  {dashboard.spendingByBranch.map((branch) => (
                    <li key={branch.branchId}>
                      <strong>{branch.branchName}</strong>
                      <span>{formatMoney(branch.totalValue)}</span>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </div>
        </>
      ) : null}
    </section>
  );
}

export default function Dashboard() {
  const { user, tokens, isAuthenticated } = useAuth();
  const {
    dashboardOverviewEnabled,
    globalSearchEnabled,
    emailFeatureSectionsEnabled,
    emailDeliveryEnabled,
    emailTwoFactorEnabled,
  } = useFeatureAvailability();
  const isAdmin = user?.role === 'Admin';

  return (
    <section className="page-shell">
      <h1>Dashboard</h1>
      <p>Protected workspace for authenticated users with feature-gated navigation and runtime shell data.</p>

      {dashboardOverviewEnabled ? (
        <>
          <PurchaseRequestDashboardOverview />

          <div className="grid grid--2">
            <article className="card">
              <h2>Session</h2>
              <p>Status: {isAuthenticated ? 'authenticated' : 'anonymous'}</p>
              <p>Access token expires in: {tokens?.expiresIn ?? 0}s</p>
              <p>Quick search: {globalSearchEnabled ? 'enabled' : 'disabled'}</p>
            </article>

            <article className="card">
              <h2>User</h2>
              {user ? (
                <>
                  <p>{user.displayName}</p>
                  <p>{user.email}</p>
                  <p>{user.role}</p>
                </>
              ) : (
                <p>Brak załadowanego usera.</p>
              )}
            </article>
          </div>

          {emailFeatureSectionsEnabled ? (
            <article className="card">
              <h2>Runtime feature snapshot</h2>
              <div className="grid grid--cards">
                <p>Email delivery: {emailDeliveryEnabled ? 'enabled' : 'disabled'}</p>
                <p>Two-factor auth: {emailTwoFactorEnabled ? 'enabled' : 'disabled'}</p>
                <p>Admin controls: {isAdmin ? 'available' : 'hidden by role'}</p>
              </div>
            </article>
          ) : null}

          <div className="hero__actions">
            <Link className="button" to="/profile">
              Profile
            </Link>
            {isAdmin ? (
              <>
                <Link className="button button--ghost" to="/admin">
                  Admin panel
                </Link>
                <Link className="button button--ghost" to="/admin/users">
                  Users directory
                </Link>
              </>
            ) : null}
          </div>
        </>
      ) : (
        <div className="page-state">
          <h2>Dashboard overview is disabled</h2>
          <p>This environment hides the dashboard shell via runtime config.</p>
        </div>
      )}
    </section>
  );
}
