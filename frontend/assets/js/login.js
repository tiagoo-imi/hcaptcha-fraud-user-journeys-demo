import { login, setAuthState } from "./auth.js";
import { showError } from "./ui.js";
import { HcaptchaDemoModal } from "./hcaptcha-result-modal.js";

function getValue(id) {
  return document.getElementById(id)?.value?.trim() || "";
}

window.onLoginSubmit = async function (token) {
  const email = getValue("email");
  const password = document.getElementById("password")?.value || "";

  if (!email || !password) {
    showError("Email and password are required.");
    window.hcaptcha?.reset();
    return;
  }

   try {
      const res = await login({
        email,
        password,
        hcaptchaToken: token
      });
  
 
      if (res.hcaptcha_response) {
        await HcaptchaDemoModal.showAsync(res.hcaptcha_response);
      }
  
      if (!res.ok) {
        showError(res.message || "Login failed.");1
        window.hcaptcha?.reset();
        return;
      }
  
      console.log(res);
      setAuthState({ email: res.email, fullName: res.full_name });
      window.location.href = "app.html";
    } catch (err) {
      showError(err?.message || "Login failed.");
      window.hcaptcha?.reset();
    }
};


