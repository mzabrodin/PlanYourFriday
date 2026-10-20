import type { Api } from './api'
import { httpApi } from './httpApi'
import { mockApi } from './mockApi'

export const usingMocks = import.meta.env.VITE_USE_MOCKS === 'true'

export const api: Api = usingMocks ? mockApi : httpApi

export { ApiError } from './client'
export type * from './types'
