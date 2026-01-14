import { apiPost } from "./api.js";

const AUTH_STATE_KEY = "demo_auth_state_v1";

/**
 * We keep minimal UI state in localStorage (email + displayName) for client UX only.
 * Server remains source-of-truth via cookies.
 */
export function getAuthState() {
  try {
    return JSON.parse(localStorage.getItem(AUTH_STATE_KEY) || "null");
  } catch {
    return null;
  }
}

export function setAuthState(state) {
  localStorage.setItem(AUTH_STATE_KEY, JSON.stringify(state));
}

export function clearAuthState() {
  localStorage.clear();
}

export async function signup({ email, password, fullName, hcaptchaToken }) {
  const res = await apiPost("/api/signup", { email, password, fullName, hcaptchaToken });

  return {
    ok: !!res?.ok,
    message: res?.message ?? null,
    hcaptcha_response: res?.hcaptcha_response ?? null,

    user_id: res?.user_id ?? null,
    email: res?.email ?? email ?? null,
    full_name: res?.full_name ?? fullName ?? ""
  };
}


export async function login({ email, password, hcaptchaToken }) {
 const res = await apiPost("/api/login", { email, password, hcaptchaToken });

  return {
    ok: !!res?.ok,
    message: res?.message ?? null,
    hcaptcha_response: res?.hcaptcha_response ?? null,

    user_id: res?.user_id ?? null,
    email: res?.email ?? email ?? null,
    full_name: res?.full_name ?? null
  };
}

export async function logout() {

  let token;
  try {
    const result = await window.hcaptcha.execute({ async: true });
    console.log(result);
    token = result.response;
    await apiPost("/api/session/end", { hcaptchaToken: token });
    clearAuthState();
    window.hcaptcha.reset();
  } catch (e) {
    console.log(e);
  }
}

export async function resetPassword({ currentPassword, newPassword, hcaptchaToken }) {
   const res = await apiPost("/api/password/reset", { currentPassword, newPassword, hcaptchaToken });

  return {
    ok: !!res?.ok,
    message: res?.message ?? null,
    hcaptcha_response: res?.hcaptcha_response ?? null,
  };
}

export function requireAuth(redirectTo = "login.html") {
  const st = getAuthState();
  if (!st) window.location.href = redirectTo;
}
