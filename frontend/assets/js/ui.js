export function $(sel, root = document) {
  return root.querySelector(sel);
}

export function $all(sel, root = document) {
  return Array.from(root.querySelectorAll(sel));
}

export function show(el) {
  if (!el) return;
  el.classList.remove("d-none");
}

export function hide(el) {
  if (!el) return;
  el.classList.add("d-none");
}

export function setText(el, text) {
  if (!el) return;
  el.textContent = text ?? "";
}

export function setHtml(el, html) {
  if (!el) return;
  el.innerHTML = html ?? "";
}

export function formatMoney(amount, currency = "USD") {
  const n = Number(amount || 0);
  return new Intl.NumberFormat(undefined, { style: "currency", currency }).format(n);
}

export function toast(message, type = "info") {
  const wrap = document.createElement("div");
  wrap.className = "toast align-items-center text-bg-" + (type === "error" ? "danger" : type) + " border-0";
  wrap.setAttribute("role", "alert");
  wrap.setAttribute("aria-live", "assertive");
  wrap.setAttribute("aria-atomic", "true");
  wrap.innerHTML = `
    <div class="d-flex">
      <div class="toast-body">${escapeHtml(message)}</div>
      <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
    </div>
  `;

  let container = document.querySelector("#toastContainer");
  if (!container) {
    container = document.createElement("div");
    container.id = "toastContainer";
    container.className = "toast-container position-fixed top-0 end-0 p-3";
    document.body.appendChild(container);
  }

  container.appendChild(wrap);
  const t = new bootstrap.Toast(wrap, { delay: 4000 });
  t.show();
  wrap.addEventListener("hidden.bs.toast", () => wrap.remove());
}

function escapeHtml(s) {
  return String(s ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

export function showError(message) {
  toast(message, "error");
}
