import { initCommon } from "./page-common.js";
import { requireAuth } from "./auth.js";
import { onResetPasswordSubmit } from "./reset-password.js";
import { onCheckoutSubmit } from "./checkout.js";
import { getCart, cartCount, cartTotal, addToCart } from "./cart.js";
import { $, show, hide,  formatMoney, toast } from "./ui.js";

const products = [
    { sku: "SKU-001", name: "Demo Sneakers", price: 79.99, desc: "Popular item. Great for testing cart + checkout." },
    { sku: "SKU-002", name: "Demo Hoodie", price: 49.50, desc: "A simple product to generate journey events." },
    { sku: "SKU-003", name: "Demo Watch", price: 149.00, desc: "Higher value item for fraud scenarios." },
];

export function renderProducts() {
    const grid = $("#productGrid");
    grid.innerHTML = "";
    for (const p of products) {
        const col = document.createElement("div");
        col.className = "col-md-6";
        col.innerHTML = `
        <div class="card h-100">
          <div class="card-body">
            <h6 class="card-title mb-1">${escapeHtml(p.name)}</h6>
            <div class="text-muted small mb-2">${escapeHtml(p.desc)}</div>
            <div class="d-flex align-items-center justify-content-between">
              <div class="fw-semibold">${formatMoney(p.price)}</div>
              <button class="btn btn-sm btn-primary" data-sku="${p.sku}">Add to cart</button>
            </div>
          </div>
        </div>
      `;
        grid.appendChild(col);
    }

    grid.querySelectorAll("button[data-sku]").forEach(btn => {
        btn.addEventListener("click", async () => {
            const sku = btn.getAttribute("data-sku");
            const item = products.find(x => x.sku === sku);
            if (!item) return;

            btn.disabled = true;

            try {
                await addToCart({
                    sku: item.sku,
                    name: item.name,
                    price: item.price,
                    qty: 1
                });

                toast(`Added "${item.name}" to cart`, "success");
                refreshCartUI();

            } catch (err) {
                console.error(err);
                toast("Failed to add item to cart", "error");
            } finally {
                btn.disabled = false;
            }
        });
    });

}

function refreshCartUI() {
    const items = getCart();
    const body = $("#cartBody");
    const empty = $("#cartEmpty");
    const table = $("#cartTable");
    const totalEl = $("#cartTotal");

    body.innerHTML = "";
    if (!items.length) {
        show(empty);
        table.classList.add("d-none");
    } else {
        hide(empty);
        table.classList.remove("d-none");
        for (const it of items) {
            const tr = document.createElement("tr");
            const subtotal = Number(it.price) * Number(it.qty || 1);
            tr.innerHTML = `
          <td>${escapeHtml(it.name)} <div class="text-muted small">${escapeHtml(it.sku)}</div></td>
          <td class="text-end">${formatMoney(it.price)}</td>
          <td class="text-end">${Number(it.qty || 1)}</td>
          <td class="text-end">${formatMoney(subtotal)}</td>
        `;
            body.appendChild(tr);
        }
    }

    totalEl.textContent = formatMoney(cartTotal());

    // Update navbar badge
    const badge = document.getElementById("navCartCount");
    if (badge) badge.textContent = String(cartCount());
}

function escapeHtml(s) {
    return String(s ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

async function boot() {
    await initCommon();
    requireAuth("login.html");
    renderProducts();
    refreshCartUI();

    document.getElementById("btnPay")?.addEventListener("click", (e) => {
        e.preventDefault();
        onCheckoutSubmit().then(() => { refreshCartUI() });
    });

    document.getElementById("btnResetPwd")?.addEventListener("click", (e) => {
        e.preventDefault();
        onResetPasswordSubmit();
    });

}

boot();