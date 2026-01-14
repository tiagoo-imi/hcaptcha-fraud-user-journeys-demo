import { ensureSession } from "./session.js";
import { getAuthState, logout } from "./auth.js";
import { cartCount } from "./cart.js";
import { $, setText } from "./ui.js";

export async function initCommon({ showAuthLinks = true } = {}) {
  await ensureSession();
  renderNavbar({ showAuthLinks });
  wireLogout();
}

export function redirectIfLoggedIn(to = "app.html") {
  const st = getAuthState();
  if (st) {
    window.location.replace(to);
    return true;
  }
  return false;
}

export function redirectIfNotLoggedIn(to = "index.html") {
  const st = getAuthState();
  if (!st) {
    window.location.replace(to);
    return true;
  }
  return false;
}

function renderNavbar({ showAuthLinks }) {
  const st = getAuthState();
  const authArea = $("#navAuthArea");
  const cartBadge = $("#navCartCount");
  const cartLink = document.getElementById("navCartLink");

  if (!authArea || !showAuthLinks) return;

  if (st) {
     if (cartBadge) cartBadge.textContent = String(cartCount());
    if (cartLink) cartLink.classList.remove("d-none");
    authArea.innerHTML = `
      <span class="navbar-text me-3">
        Signed in as <span class="code-badge">${escapeHtml(st.email || "user")}</span>
      </span>
      <a class="btn btn-outline-light btn-sm me-2" href="app.html">Dashboard</a>
      <button class="btn btn-warning btn-sm" id="btnLogout">Log out</button>
    `;
  } else {
     if (cartBadge) cartBadge.textContent = "0";
      if (cartLink) cartLink.classList.add("d-none");
    authArea.innerHTML = ``;
  }
}

function wireLogout() {
  const btn = document.getElementById("btnLogout");
  if (!btn) return;
  btn.addEventListener("click", async () => {
    console.log("Logging out...");
    btn.disabled = true;
    try {
      await logout();
      window.location.href = "index.html";
    } finally {
      btn.disabled = false;
    }
  });
}

function escapeHtml(s) {
  return String(s ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}
