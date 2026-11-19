(() => {
    const modalElement = document.getElementById("genericModal");
    const modalContent = document.getElementById("modalContent");
    const modal = modalElement ? bootstrap.Modal.getOrCreateInstance(modalElement) : null;

    function showMessage(message, isError) {
        let container = document.getElementById("crudMessage");
        if (!container) {
            container = document.createElement("div");
            container.id = "crudMessage";
            container.setAttribute("role", "status");
            document.querySelector("main")?.prepend(container);
        }
        container.className = `alert ${isError ? "alert-danger" : "alert-success"}`;
        container.textContent = message;
        window.setTimeout(() => container.classList.add("d-none"), 5000);
    }

    function parseValidation(form) {
        if (window.jQuery?.validator?.unobtrusive) {
            window.jQuery.validator.unobtrusive.parse(form);
        }
    }

    function loadModal(url) {
        if (!modal || !modalContent) return;
        window.jQuery.ajax({
            url,
            headers: { "X-Requested-With": "XMLHttpRequest" }
        }).done(html => {
            modalContent.innerHTML = html;
            parseValidation(modalContent);
            modal.show();
        }).fail(() => showMessage("Không thể tải nội dung. Vui lòng thử lại.", true));
    }

    function refreshRows() {
        const table = document.querySelector("table[data-rows-url]");
        if (!table) return;
        window.jQuery.get(table.dataset.rowsUrl)
            .done(html => {
                const body = table.tBodies[0];
                if (body) body.innerHTML = html;
            })
            .fail(() => showMessage("Đã lưu dữ liệu nhưng không thể làm mới danh sách.", true));
    }

    document.addEventListener("click", event => {
        const button = event.target.closest("#btnCreate, .btn-detail, .btn-edit, .btn-delete");
        if (!button) return;
        event.preventDefault();
        if (button.dataset.url) loadModal(button.dataset.url);
    });

    document.addEventListener("submit", event => {
        const form = event.target;
        if (!(form instanceof HTMLFormElement) ||
            (!form.closest("#modalContent") && form.dataset.ajaxForm !== "true")) return;
        event.preventDefault();

        if (window.jQuery?.validator && !window.jQuery(form).valid()) return;

        const payload = new FormData(form);
        window.jQuery.ajax({
            url: form.action,
            method: form.method || "POST",
            data: payload,
            processData: false,
            contentType: false,
            headers: { "X-Requested-With": "XMLHttpRequest" }
        }).done(result => {
            if (!result?.success) {
                showMessage(result?.message || "Không thể hoàn tất thao tác.", true);
                return;
            }
            if (modal && form.closest("#modalContent")) modal.hide();
            showMessage(result.message || "Đã lưu thay đổi.", false);
            if (form.dataset.refresh !== "false") refreshRows();
        }).fail(xhr => {
            if (xhr.status === 422 && modalContent && form.closest("#modalContent")) {
                modalContent.innerHTML = xhr.responseText;
                parseValidation(modalContent);
                return;
            }
            const response = xhr.responseJSON;
            showMessage(response?.message || "Thao tác thất bại. Vui lòng thử lại.", true);
        });
    });
})();
