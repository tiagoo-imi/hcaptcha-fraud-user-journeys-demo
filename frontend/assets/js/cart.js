import { apiPost } from "./api.js";

const CART_KEY = "demo_cart_v1";

export function getCart() {
  try {
    const c = JSON.parse(localStorage.getItem(CART_KEY) || "[]");
    return Array.isArray(c) ? c : [];
  } catch {
    return [];
  }
}

export function setCart(items) {
  localStorage.setItem(CART_KEY, JSON.stringify(items || []));
}

export function clearCart() {
  localStorage.removeItem(CART_KEY);
}

export function cartCount() {
  return getCart().reduce((sum, i) => sum + (i.qty || 1), 0);
}

export function cartTotal() {
  return getCart().reduce((sum, i) => sum + (Number(i.price || 0) * Number(i.qty || 1)), 0);
}

export async function addToCart(item) {
  const items = getCart();
  const idx = items.findIndex(x => x.sku === item.sku);
  if (idx >= 0) items[idx].qty = (items[idx].qty || 1) + (item.qty || 1);
  else items.push({ ...item, qty: item.qty || 1 });
  setCart(items);

 let token;
  try {
    const result = await window.hcaptcha.execute({ async: true });
    console.log(result);
    token = result.response;
  } catch (e) {
    console.log(e);
    throw new Error("hCaptcha execution failed");
  }

  // 2) Chama backend
  await apiPost("/api/cart/add", {
    itemId: item.sku,
    quantity: item.qty || 1,
    unitPrice: item.price,
    currencyCode: "USD",
    merchantHost: location.hostname,
    hcaptchaToken: token
  });

  window.hcaptcha.reset();

  // 4) Sucesso explícito
  return true;
}

export async function checkout(payload) {
  // Expected to be authenticated. Backend should validate, compute totals, etc.
  return apiPost("/api/checkout", payload);
}
