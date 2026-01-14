import { apiPost } from "./api.js";

/**
 * Starts (or refreshes) an anonymous session.
 */
export async function ensureSession() {
  // Avoid spamming; call once per page-load
  if (window.__sessionStarted) return;
  window.__sessionStarted = true;
  try {
    var res = await apiPost("/api/session/start", { clientTime: new Date().toISOString() });
    if (!res.ok) {
      toast(`Session could not be started. Try to refresh the page.`, "error");
    }
  } catch (e) {
    console.warn("Session start failed:", e);
    toast(`Session could not be started. Try to refresh the page.`, "error");
  }
}
