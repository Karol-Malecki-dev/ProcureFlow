import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { vi } from 'vitest';
import Notifications from '../../pages/Notifications';
import { useNotifications } from '../../hooks/useNotifications';
import { NotificationType } from '../../types';

vi.mock('../../hooks/useNotifications');

const mockedUseNotifications = useNotifications as jest.MockedFunction<typeof useNotifications>;

describe('Notifications page', () => {
  it('does not expose removed project-task navigation', () => {
    const markAsRead = jest.fn().mockResolvedValue(undefined);
    mockedUseNotifications.mockReturnValue({
      notifications: [{
        id: 'notification-1',
        type: NotificationType.TaskDeadlineApproaching,
        title: 'Task deadline approaching',
        message: 'Review the release notes.',
        resourceType: null,
        resourceId: 'task-1',
        createdAt: '2026-07-31T10:00:00Z',
        readAt: null,
        isRead: false,
      }],
      unreadCount: 1,
      loading: false,
      error: null,
      refreshNotifications: jest.fn(),
      markAsRead,
      markAllAsRead: jest.fn(),
    });

    render(<MemoryRouter><Notifications /></MemoryRouter>);

    expect(screen.queryByRole('button', { name: 'Open task' })).not.toBeInTheDocument();
    expect(markAsRead).not.toHaveBeenCalled();
  });
});