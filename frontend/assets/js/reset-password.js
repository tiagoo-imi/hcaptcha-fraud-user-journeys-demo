import { resetPassword } from "./auth.js";
import { showError } from "./ui.js";
import { HcaptchaDemoModal } from "./hcaptcha-result-modal.js";


export async function onResetPasswordSubmit() {

    const currentPassword = document.getElementById("currentPassword")?.value || "";
    const newPassword = document.getElementById("newPassword")?.value || "";

    if (!newPassword || !currentPassword) {
        showError("Current and new passwords are required.");
        window.hcaptcha?.reset();
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


    try {
        const res = await resetPassword({
            currentPassword,
            newPassword,
            hcaptchaToken: token
        });

        if (res.hcaptcha_response) {
            await HcaptchaDemoModal.showAsync(res.hcaptcha_response);
        }

        if (!res.ok) {
            showError(res.message || "Failed to reset password.");
            window.hcaptcha?.reset();
            return;
        }

    } catch (err) {
        showError(err?.message || "Failed to reset password.");
        window.hcaptcha?.reset();
    }
};



