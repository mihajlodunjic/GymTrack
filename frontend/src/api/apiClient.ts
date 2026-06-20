import { ApiErrorResponse } from "../types/api";

const FALLBACK_API_BASE_URL = "https://localhost:7250";
const API_BASE_URL = (
  import.meta.env.VITE_API_BASE_URL || FALLBACK_API_BASE_URL
).replace(/\/+$/, "");

const TOKEN_STORAGE_KEY = "gymtrack.jwt";

let unauthorizedHandler: (() => void) | null = null;

export class ApiError extends Error {
  statusCode: number;
  errors?: Record<string, string[]>;

  constructor(message: string, statusCode: number, errors?: Record<string, string[]>) {
    super(message);
    this.name = "ApiError";
    this.statusCode = statusCode;
    this.errors = errors;
  }
}

type RequestMethod = "GET" | "POST" | "PUT" | "DELETE";
type ResponseType = "json" | "blob" | "text" | "void";

interface RequestOptions {
  method?: RequestMethod;
  body?: unknown;
  headers?: HeadersInit;
  responseType?: ResponseType;
  signal?: AbortSignal;
  skipAuth?: boolean;
}

export const getApiBaseUrl = () => API_BASE_URL;

export const getStoredToken = () => localStorage.getItem(TOKEN_STORAGE_KEY);

export const setStoredToken = (token: string) => {
  localStorage.setItem(TOKEN_STORAGE_KEY, token);
};

export const clearStoredToken = () => {
  localStorage.removeItem(TOKEN_STORAGE_KEY);
};

export const setUnauthorizedHandler = (handler: (() => void) | null) => {
  unauthorizedHandler = handler;
};

export const getApiErrorMessages = (error: unknown) => {
  if (error instanceof ApiError && error.errors) {
    return Object.values(error.errors).flat();
  }

  if (error instanceof Error) {
    return [error.message];
  }

  return ["Unexpected error."];
};

export async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { method = "GET", body, headers, responseType = "json", signal, skipAuth = false } = options;

  const requestHeaders = new Headers(headers);
  const token = getStoredToken();

  if (!skipAuth && token) {
    requestHeaders.set("Authorization", `Bearer ${token}`);
  }

  const isJsonBody = body !== undefined && body !== null && !(body instanceof FormData);
  if (isJsonBody) {
    requestHeaders.set("Content-Type", "application/json");
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method,
    headers: requestHeaders,
    body: isJsonBody ? JSON.stringify(body) : (body as BodyInit | null | undefined),
    signal,
  });

  if (!response.ok) {
    let payload: ApiErrorResponse | null = null;
    const contentType = response.headers.get("content-type") || "";

    if (contentType.includes("application/json")) {
      payload = (await response.json()) as ApiErrorResponse;
    }

    if (response.status === 401) {
      clearStoredToken();
      unauthorizedHandler?.();
    }

    throw new ApiError(
      payload?.message || `Request failed with status ${response.status}.`,
      payload?.statusCode || response.status,
      payload?.errors
    );
  }

  if (responseType === "void") {
    return undefined as T;
  }

  if (responseType === "blob") {
    return (await response.blob()) as T;
  }

  if (responseType === "text") {
    return (await response.text()) as T;
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}
