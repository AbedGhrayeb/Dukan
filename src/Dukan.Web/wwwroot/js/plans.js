// دكان — Admin Plans single-page CRUD via AJAX.
(() => {
    'use strict';

    const $ = window.jQuery;

    $(function () {
        const container = $('#plansTableContainer');
        if (container.length === 0) return;

        const alertBox = $('#plansAlert');
        const planModalEl = document.getElementById('planModal');
        const planForm = document.getElementById('planForm');
        const planModal = bootstrap.Modal.getOrCreateInstance(planModalEl);
        const deleteModalEl = document.getElementById('deletePlanModal');
        const deleteModal = bootstrap.Modal.getOrCreateInstance(deleteModalEl);

        const tableUrl = container.data('table-url');
        const createUrl = planForm.dataset.createUrl;
        const editUrlPrefix = planForm.dataset.editUrlPrefix;
        const token = $('input[name="__RequestVerificationToken"]').first().val();

        function showAlert(message, isError) {
            alertBox
                .removeClass('d-none alert-success alert-danger')
                .addClass(isError ? 'alert-danger' : 'alert-success')
                .text(message)
                .stop(true, true)
                .show()
                .delay(5000)
                .fadeOut(400);
        }

        function refreshTable() {
            return $.get(tableUrl).done((html) => {
                container.html(html);
            });
        }

        function clearErrors() {
            planForm.querySelectorAll('.field-validation-error').forEach((el) => { el.textContent = ''; });
            planForm.querySelectorAll('.input-validation-error').forEach((el) => el.classList.remove('input-validation-error'));
            const errorBox = document.getElementById('planFormError');
            errorBox.classList.add('d-none');
            errorBox.textContent = '';
            const validator = $(planForm).data('validator');
            if (validator) validator.resetForm();
        }

        function applyErrors(errors) {
            clearErrors();
            if (!errors) return;
            Object.entries(errors).forEach(([field, messages]) => {
                const input = planForm.querySelector(`[name="${field}"]`);
                if (input) input.classList.add('input-validation-error');
                const msgEl = planForm.querySelector(`[data-valmsg-for="${field}"]`);
                if (msgEl) msgEl.textContent = messages.join(' ');
            });
        }

        function resetForm() {
            planForm.reset();
            clearErrors();
            planForm.querySelector('[name="Id"]').value = '';
            planForm.querySelector('[name="Currency"]').value = 'ILS';
            planForm.querySelector('[name="DurationUnit"]').value = 'Month';
            planForm.querySelector('[name="IsActive"]').checked = true;
        }

        function setSubmitting(submitting) {
            document.getElementById('planFormSubmit').disabled = submitting;
            document.getElementById('planFormSpinner').classList.toggle('d-none', !submitting);
        }

        function openPlanModal(title, subtitle, submitLabel) {
            document.getElementById('planModalTitle').textContent = title;
            document.getElementById('planModalSubtitle').textContent = subtitle;
            document.getElementById('planFormSubmitText').textContent = submitLabel;
            planModal.show();
        }

        planModalEl.addEventListener('shown.bs.modal', () => {
            const modalBody = planForm.querySelector('.modal-body');
            if (modalBody) modalBody.scrollTop = 0;
            const nameInput = planForm.querySelector('[name="Name"]');
            if (nameInput) {
                nameInput.focus();
                nameInput.select();
            }
        });

        planForm.addEventListener('keydown', (event) => {
            if ((event.ctrlKey || event.metaKey) && event.key === 'Enter') {
                event.preventDefault();
                $(planForm).trigger('submit');
            }
        });

        const durationUnitNames = ['Day', 'Week', 'Month', 'Year'];

        function fillForm(plan) {
            planForm.querySelector('[name="Id"]').value = plan.id || '';
            planForm.querySelector('[name="Name"]').value = plan.name || '';
            planForm.querySelector('[name="Duration"]').value = plan.duration ?? '';
            const unit = plan.durationUnit;
            planForm.querySelector('[name="DurationUnit"]').value =
                typeof unit === 'string' ? unit : (durationUnitNames[unit] ?? '');
            planForm.querySelector('[name="Price"]').value = plan.price ?? '';
            planForm.querySelector('[name="Currency"]').value = plan.currency || '';
            planForm.querySelector('[name="DisplayOrder"]').value = plan.displayOrder ?? '';
            planForm.querySelector('[name="IsTrial"]').checked = !!plan.isTrial;
            planForm.querySelector('[name="IsActive"]').checked = !!plan.isActive;
            planForm.querySelector('[name="Description"]').value = plan.description || '';
        }

        // إنشاء خطة جديدة
        $('#createPlanBtn').on('click', () => {
            resetForm();
            openPlanModal('خطة جديدة', 'أضف خطة اشتراك جديدة لعرضها على العملاء.', 'حفظ الخطة');
        });

        // تعديل خطة
        container.on('click', '.js-plan-edit', (event) => {
            $.get(event.currentTarget.dataset.url)
                .done((res) => {
                    if (!res || !res.success) {
                        showAlert((res && res.message) || 'تعذر تحميل بيانات الخطة.', true);
                        return;
                    }
                    fillForm(res.plan);
                    openPlanModal('تعديل الخطة', 'عدّل تفاصيل الخطة ثم احفظ التغييرات.', 'حفظ التعديلات');
                })
                .fail(() => showAlert('تعذر الاتصال بالخادم. حاول مرة أخرى.', true));
        });

        // إرسال النموذج (إنشاء / تعديل)
        $(planForm).on('submit', (event) => {
            event.preventDefault();
            if (!$(planForm).valid()) return;

            const id = planForm.querySelector('[name="Id"]').value;
            const url = id ? editUrlPrefix.replace('__ID__', id) : createUrl;

            setSubmitting(true);
            $.post(url, $(planForm).serialize())
                .done((res) => {
                    if (res && res.success) {
                        planModal.hide();
                        showAlert(res.message);
                        refreshTable();
                    } else if (res && res.errors) {
                        applyErrors(res.errors);
                    } else {
                        showAlert((res && res.message) || 'حدث خطأ غير متوقع.', true);
                    }
                })
                .fail(() => showAlert('تعذر الاتصال بالخادم. حاول مرة أخرى.', true))
                .always(() => setSubmitting(false));
        });

        // تفعيل / إيقاف
        container.on('click', '.js-plan-toggle', (event) => {
            const btn = event.currentTarget;
            btn.disabled = true;
            $.post(btn.dataset.url, { __RequestVerificationToken: token })
                .done((res) => {
                    if (res && res.success) {
                        showAlert(res.message);
                        refreshTable();
                    } else {
                        btn.disabled = false;
                        showAlert((res && res.message) || 'تعذر تغيير حالة الخطة.', true);
                    }
                })
                .fail(() => {
                    btn.disabled = false;
                    showAlert('تعذر الاتصال بالخادم. حاول مرة أخرى.', true);
                });
        });

        // حذف خطة
        let pendingDeleteUrl = null;

        container.on('click', '.js-plan-delete', (event) => {
            const btn = event.currentTarget;
            document.getElementById('deletePlanName').textContent = btn.dataset.name;
            pendingDeleteUrl = btn.dataset.url;
            deleteModal.show();
        });

        document.getElementById('confirmDeleteBtn').addEventListener('click', () => {
            if (!pendingDeleteUrl) return;
            deleteModal.hide();
            const btn = document.getElementById('confirmDeleteBtn');
            btn.disabled = true;
            document.getElementById('confirmDeleteSpinner').classList.remove('d-none');
            $.post(pendingDeleteUrl, { __RequestVerificationToken: token })
                .done((res) => {
                    if (res && res.success) {
                        showAlert(res.message);
                        refreshTable();
                    } else {
                        showAlert((res && res.message) || 'تعذر حذف الخطة.', true);
                    }
                })
                .fail(() => showAlert('تعذر الاتصال بالخادم. حاول مرة أخرى.', true))
                .always(() => {
                    pendingDeleteUrl = null;
                    btn.disabled = false;
                    document.getElementById('confirmDeleteSpinner').classList.add('d-none');
                });
        });
    });
})();
