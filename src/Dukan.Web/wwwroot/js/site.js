// دكان — site enhancements (progressive, no core business logic).
(() => {
    'use strict';

    document.addEventListener('DOMContentLoaded', () => {
        const modal = document.getElementById('confirmModal');
        if (!modal) return;

        const bsModal = new bootstrap.Modal(modal);
        const titleEl = document.getElementById('confirmModalLabel');
        const subtitleEl = document.getElementById('confirmModalSubtitle');
        const bodyEl = document.getElementById('confirmModalBody');
        const headerEl = document.getElementById('confirmModalHeader');
        const iconEl = document.getElementById('confirmModalIcon');
        const btnEl = document.getElementById('confirmModalBtn');
        const btnTextEl = document.getElementById('confirmModalBtnText');
        const spinnerEl = document.getElementById('confirmModalSpinner');
        let pendingForm = null;

        const actions = {
            approve: {
                title: 'تفعيل الاشتراك',
                subtitle: 'سيتم تفعيل هذا الطلب فوراً.',
                body: 'هل أنت متأكد من تفعيل هذا الطلب؟',
                btnText: 'تفعيل',
                btnClass: 'btn-success',
                icon: 'bi-check-circle',
                headerClass: 'plan-modal-header'
            },
            reject: {
                title: 'رفض الطلب',
                subtitle: 'لا يمكن التراجع عن هذا الإجراء.',
                body: 'هل أنت متأكد من رفض هذا الطلب؟',
                btnText: 'رفض',
                btnClass: 'btn-danger',
                icon: 'bi-x-circle',
                headerClass: 'plan-modal-header-danger'
            },
            cancel: {
                title: 'إلغاء الطلب',
                subtitle: 'سيتم إلغاء هذا الطلب نهائياً.',
                body: 'هل أنت متأكد من إلغاء هذا الطلب؟',
                btnText: 'إلغاء',
                btnClass: 'btn-danger',
                icon: 'bi-slash-circle',
                headerClass: 'plan-modal-header-danger'
            },
            'cancel-sub': {
                title: 'إلغاء الاشتراك',
                subtitle: 'سيتم إيقاف هذا الاشتراك نهائياً.',
                body: 'هل أنت متأكد من إلغاء هذا الاشتراك؟',
                btnText: 'إلغاء',
                btnClass: 'btn-danger',
                icon: 'bi-x-octagon',
                headerClass: 'plan-modal-header-danger'
            },
            renew: {
                title: 'تجديد الاشتراك',
                subtitle: 'سيتم تفعيل اشتراك جديد.',
                body: 'هل أنت متأكد من تجديد هذا الاشتراك؟',
                btnText: 'تجديد',
                btnClass: 'btn-primary',
                icon: 'bi-arrow-repeat',
                headerClass: 'plan-modal-header-info'
            }
        };

        document.querySelectorAll('form[data-confirm]').forEach((form) => {
            form.addEventListener('submit', (event) => {
                event.preventDefault();
                pendingForm = form;

                const key = form.dataset.confirm;
                const cfg = actions[key] || {
                    title: 'تأكيد',
                    subtitle: '',
                    body: key,
                    btnText: 'تأكيد',
                    btnClass: 'btn-primary',
                    icon: 'bi-question-circle',
                    headerClass: 'plan-modal-header-info'
                };

                titleEl.textContent = cfg.title;
                subtitleEl.textContent = cfg.subtitle;
                bodyEl.textContent = cfg.body;
                btnTextEl.textContent = cfg.btnText;

                // header style
                headerEl.className = 'modal-header ' + cfg.headerClass;

                // icon
                iconEl.innerHTML = '<i class="bi ' + cfg.icon + '"></i>';

                // button style
                btnEl.className = 'btn ' + cfg.btnClass;

                // reset spinner
                spinnerEl.classList.add('d-none');
                btnEl.disabled = false;

                bsModal.show();
            });
        });

        btnEl.addEventListener('click', () => {
            btnEl.disabled = true;
            spinnerEl.classList.remove('d-none');

            setTimeout(() => {
                bsModal.hide();
                if (pendingForm) {
                    pendingForm.submit();
                    pendingForm = null;
                }
            }, 300);
        });

        modal.addEventListener('hidden.bs.modal', () => {
            pendingForm = null;
            spinnerEl.classList.add('d-none');
            btnEl.disabled = false;
        });
    });
})();
