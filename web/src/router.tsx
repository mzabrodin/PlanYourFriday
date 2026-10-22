import { createBrowserRouter } from 'react-router'
import { Layout } from './components/Layout'
import { AddFriendPage } from './pages/AddFriendPage'
import { RequireAuth } from './components/RequireAuth'
import { CalendarPage } from './pages/CalendarPage'
import { CreateGroupPage } from './pages/CreateGroupPage'
import { CreatePollPage } from './pages/CreatePollPage'
import { FriendsPage } from './pages/FriendsPage'
import { GroupPage } from './pages/GroupPage'
import { GroupsPage } from './pages/GroupsPage'
import { HomePage } from './pages/HomePage'
import { IdeasPage } from './pages/IdeasPage'
import { JoinPage } from './pages/JoinPage'
import { LoginPage } from './pages/LoginPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { PollPage } from './pages/PollPage'
import { SettingsPage } from './pages/SettingsPage'
import { WheelPage } from './pages/WheelPage'

export const router = createBrowserRouter([
  {
    element: <Layout />,
    children: [
      { path: '/login', element: <LoginPage /> },
      {
        element: <RequireAuth />,
        children: [
          { path: '/', element: <HomePage /> },
          { path: '/groups', element: <GroupsPage /> },
          { path: '/calendar', element: <CalendarPage /> },
          { path: '/settings', element: <SettingsPage /> },
          { path: '/wheel', element: <WheelPage /> },
          { path: '/groups/new', element: <CreateGroupPage /> },
          { path: '/groups/:groupId', element: <GroupPage /> },
          { path: '/join/:token', element: <JoinPage /> },
          { path: '/friends', element: <FriendsPage /> },
          { path: '/add/:code', element: <AddFriendPage /> },
          { path: '/ideas', element: <IdeasPage /> },
          { path: '/ideas/new', element: <CreatePollPage /> },
          { path: '/ideas/:pollId', element: <PollPage /> },
        ],
      },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
])
