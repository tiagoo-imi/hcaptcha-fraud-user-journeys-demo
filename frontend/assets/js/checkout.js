import { apiPost } from "./api.js";
import { $, show, setText, toast } from "./ui.js";
import { getCart, cartTotal, clearCart } from "./cart.js";
import { HcaptchaDemoModal } from "./hcaptcha-result-modal.js";

function detectNetwork(cardNumber) {
    const n = (cardNumber || "").replace(/\D/g, "");
    if (!n) return null;
    if (n.startsWith("4")) return "visa";
    if (/^5[1-5]/.test(n)) return "mastercard";
    if (/^3[47]/.test(n)) return "amex";
    if (n.startsWith("6")) return "discover";
    return "unknown";
}

function toTxnItems(cartItems) {
    return (cartItems || []).slice(0, 100).map((it) => ({
        name: it.name,
        value: Number(it.price),
        quantity: Number(it.qty || 1),
    }));
}

export async function onCheckoutSubmit() {

    const chkErr = $("#chkErr");
    const chkOk = $("#chkOk");

    const items = getCart();
    if (!items.length) {
        show(chkErr);
        setText(chkErr, "Your cart is empty.");
        toast("Cart is empty", "error");
        return;
    }

    let token;
    try {
        const result = await window.hcaptcha.execute({ async: true });
        token = result.response;
    } catch (e) {
        console.log(e);
        throw new Error("hCaptcha execution failed");
    }


    const cardNumberRaw = $("#cardNumber")?.value?.trim() || "";
    const cardNumber = cardNumberRaw.replace(/\D/g, "");
    const payload = {
        hcaptchaToken: token,

        value: Number(cartTotal()),
        currencyCode: "USD",

        paymentMethod: "credit_card",
        paymentNetwork: detectNetwork(cardNumber),
        cardBin: cardNumber.slice(0, 6) || null,
        cardLastFour: cardNumber.slice(-4) || null,

        shippingValue: 0,

        billingAddress: {
            recipient: ($("#cardName")?.value?.trim() || email),
            address_1: ($("#address")?.value?.trim() || null),
            postal_code: ($("#zip")?.value?.trim() || null),
            region_code: "USA"
        },

        shippingAddress: null,

        items: toTxnItems(items),
    };
    const btn = $("#btnPay");
    btn.disabled = true;
    btn.textContent = "Processing...";

    try {

        var res = await apiPost("/api/checkout", payload);

        if (res.hcaptcha_response) {
            await HcaptchaDemoModal.showAsync(res.hcaptcha_response);
        }

        if (!res.ok) {
            show(chkErr);
            setText(chkErr, res.message || "Checkout failed.");
            toast(res.message || "Checkout failed", "error");
            window.hcaptcha?.reset();
            return;
        }

        show(chkOk);
        setText(chkOk, "Payment approved. Order placed.");
        toast("Checkout successful", "success");
        clearCart();

        $("#formCheckout").reset();
    } catch (err) {
        show(chkErr);
        setText(chkErr, err.message || "Checkout failed.");
        toast(err.message || "Checkout failed", "error");

    } finally {
        btn.disabled = false;
        btn.textContent = "Pay now";
    }
};




