export const HcaptchaDemoModal = (() => {
  const MODAL_ID = "hcaptcha-demo-modal";

  function ensureModal() {
    let modal = document.getElementById(MODAL_ID);
    if (modal) return modal;

    const el = document.createElement("div");
    el.className = "modal fade";
    el.id = MODAL_ID;
    el.tabIndex = -1;

    el.innerHTML = `
<div class="modal-dialog modal-lg modal-dialog-centered modal-dialog-scrollable">
  <div class="modal-content">
    <div class="modal-header">
      <h5 class="modal-title">hCaptcha Decision (Demo)</h5>
      <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
    </div>
    <div class="modal-body">
      <div id="hcm-status" class="mb-3"></div>
      <table class="table table-sm table-bordered align-middle mb-3">
        <tbody id="hcm-table"></tbody>
      </table>
    </div>
    <div class="modal-footer">
      <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">
        Close
      </button>
    </div>
  </div>
</div>`;
    document.body.appendChild(el);
    return el;
  }

  function badge(text, type) {
    return `<span class="badge bg-${type} me-1">${text}</span>`;
  }

  function renderStatus(dto) {
    const items = [];
    items.push(dto.hcaptcha_approved ? badge("APPROVED", "success") : badge("NOT APPROVED", "danger"));
    if (dto.bot_detected) items.push(badge("BOT", "danger"));
    if (dto.fraud_detected) items.push(badge("FRAUD", "danger"));
    if (dto.account_takeover_suspected) items.push(badge("ATO", "warning"));
    return items.join("");
  }

  function row(key, value) {
    if (value === null || value === undefined) value = "—";
    if (typeof value === "boolean") value = value ? "true" : "false";
    return `
<tr>
  <th class="bg-light text-nowrap">${key}</th>
  <td>${value}</td>
</tr>`;
  }

  function fill(dto) {
    const modalEl = ensureModal();
    modalEl.querySelector("#hcm-status").innerHTML = renderStatus(dto);

    const rows = [
      ["success", dto.success],
      ["hcaptcha_approved", dto.hcaptcha_approved],
      ["bot_detected", dto.bot_detected],
      ["risk_score", dto.risk_score?.toFixed?.(3)],
      ["fraud_detected", dto.fraud_detected],
      ["fraud_score", dto.fraud_score?.toFixed?.(3)],
      ["account_takeover_suspected", dto.account_takeover_suspected],
      ["similarity", dto.similarity?.toFixed?.(3)],
      ["similarity_indicators", dto.similarity_indicators?.join(", ")]
    ];

    modalEl.querySelector("#hcm-table").innerHTML = rows.map(r => row(r[0], r[1])).join("");
    return modalEl;
  }

  function show(dto) {
    const modalEl = fill(dto);
    const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    modal.show();
  }

  function showAsync(dto) {
    const modalEl = fill(dto);
    const modal = bootstrap.Modal.getOrCreateInstance(modalEl);

    return new Promise((resolve) => {
      // garante que resolve só uma vez
      const handler = () => {
        modalEl.removeEventListener("hidden.bs.modal", handler);
        resolve();
      };
      modalEl.addEventListener("hidden.bs.modal", handler);
      modal.show();
    });
  }

  return { show, showAsync };
})();
