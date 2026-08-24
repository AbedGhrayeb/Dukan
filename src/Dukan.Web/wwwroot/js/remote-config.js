/* Remote Config draft + publish — mirrors plans.js pattern */
(function () {
    'use strict';

    console.log('[remote-config] script loaded, jQuery:', typeof window.jQuery, 'bootstrap:', typeof window.bootstrap);
    const $ = window.jQuery;
    if (!$) {
        console.error('[remote-config] jQuery not found!');
        return;
    }

    $(function () {
        console.log('[remote-config] DOM ready, urls:', window.rcUrls);
        const urls = window.rcUrls || {};
        const $alert = $('#rcAlert');
        const $tableContainer = $('#rcTableContainer');
        const tableUrl = $tableContainer.data('table-url');
        const rcModalEl = document.getElementById('rcModal');
        const rcModal = rcModalEl ? bootstrap.Modal.getOrCreateInstance(rcModalEl) : null;
        const deleteModalEl = document.getElementById('deleteRcModal');
        const deleteModal = deleteModalEl ? bootstrap.Modal.getOrCreateInstance(deleteModalEl) : null;

        let editingKey = null;
        let pendingDeleteKey = null;

        function showAlert(type, msg) {
            if ($alert.length === 0) {
                // fallback to alert if container missing
                alert(msg);
                return;
            }
            $alert.removeClass('d-none alert-success alert-danger alert-warning alert-info')
                .addClass('alert-' + type)
                .text(msg)
                .stop(true, true)
                .removeClass('d-none')
                .show();
            if (type === 'success') {
                $alert.delay(5000).fadeOut(400, () => $alert.addClass('d-none').removeAttr('style'));
            }
            window.scrollTo({ top: 0, behavior: 'smooth' });
        }

        function getErrorsHtml(errors) {
            if (!errors) return '';
            return Object.entries(errors).map(([k, msgs]) => `${k}: ${msgs.join(', ')}`).join(' | ');
        }

        function fullReload() { window.location.reload(); }

        // Create
        $('#createRcBtn').on('click', () => {
            editingKey = null;
            const form = document.getElementById('rcForm');
            if (form) form.reset();
            $('#rcKey').prop('readonly', false).val('');
            $('#rcValue').val('');
            $('#rcDescription').val('');
            $('#rcValueType').val('string');
            $('#rcFormSummary').addClass('d-none').text('');
            $('#rcModalLabel').text('إضافة مفتاح جديد');
            $('#rcSubmitText').text('حفظ في المسودة');
            // clear validation
            const $form = $('#rcForm');
            if ($form.data('validator')) $form.data('validator').resetForm();
            $form.find('.input-validation-error').removeClass('input-validation-error');
            rcModal && rcModal.show();
        });

        // Edit
        $(document).on('click', '.js-rc-edit', function () {
            const key = $(this).data('key');
            if (!key) return;
            $.get(urls.get, { key }).done(res => {
                if (!res.success) { showAlert('danger', res.message || 'تعذر جلب المفتاح.'); return; }
                const p = res.param;
                editingKey = p.key;
                $('#rcKey').val(p.key).prop('readonly', true);
                $('#rcValue').val(p.value);
                $('#rcDescription').val(p.description || '');
                $('#rcValueType').val(p.valueType || 'string');
                $('#rcFormSummary').addClass('d-none').text('');
                $('#rcModalLabel').text('تعديل المفتاح: ' + p.key);
                $('#rcSubmitText').text('حفظ في المسودة');
                const $form = $('#rcForm');
                if ($form.data('validator')) $form.data('validator').resetForm();
                rcModal && rcModal.show();
            }).fail(xhr => {
                const msg = xhr.responseJSON?.message || xhr.responseText || 'تعذر جلب المفتاح.';
                showAlert('danger', msg);
            });
        });

        // Submit upsert (draft)
        $('#rcForm').on('submit', function (e) {
            e.preventDefault();
            console.log('[remote-config] submit fired');
            const $form = $(this);
            if ($form.data('validator') && !$form.valid()) {
                console.log('[remote-config] client validation failed', $form.validate().errorList);
                const msg = $form.validate().errorList.map(e => e.message).join(' | ') || 'تحقق من الحقول المطلوبة.';
                $('#rcFormSummary').removeClass('d-none').text(msg);
                showAlert('danger', msg);
                return;
            }

            const $btn = $('#rcSubmitBtn');
            const $spinner = $('#rcSubmitSpinner');
            $btn.prop('disabled', true); $spinner.removeClass('d-none');
            $('#rcFormSummary').addClass('d-none');

            if (editingKey) $('#rcKey').val(editingKey);

            const data = $form.serialize();
            console.log('[remote-config] POST upsert', urls.upsert, data);
            $.post(urls.upsert, data).done(res => {
                console.log('[remote-config] upsert response', res);
                // session expired returns HTML, not JSON
                if (typeof res === 'string' && res.includes('<!DOCTYPE')) {
                    showAlert('danger', 'انتهت الجلسة، يرجى تسجيل الدخول مرة أخرى ثم المحاولة.');
                    $('#rcFormSummary').removeClass('d-none').text('انتهت الجلسة، يرجى تسجيل الدخول.');
                    return;
                }
                if (res.success) {
                    rcModal && rcModal.hide();
                    showAlert('success', res.message);
                    setTimeout(fullReload, 600);
                } else {
                    if (res.errors) {
                        const msg = getErrorsHtml(res.errors);
                        $('#rcFormSummary').removeClass('d-none').text(msg);
                        showAlert('danger', msg);
                    } else {
                        const msg = res.message || 'حدث خطأ غير متوقع.';
                        $('#rcFormSummary').removeClass('d-none').text(msg);
                        showAlert('danger', msg);
                    }
                }
            }).fail(xhr => {
                console.error('[remote-config] upsert failed', xhr);
                if (xhr.responseText && xhr.responseText.includes('<!DOCTYPE')) {
                    const msg = 'انتهت الجلسة، يرجى تسجيل الدخول مرة أخرى.';
                    $('#rcFormSummary').removeClass('d-none').text(msg);
                    showAlert('danger', msg);
                    return;
                }
                const res = xhr.responseJSON;
                let msg = res?.message || '';
                if (res?.errors) msg = getErrorsHtml(res.errors);
                if (!msg) msg = xhr.responseText ? xhr.responseText.substring(0, 500) : 'حدث خطأ غير متوقع.';
                $('#rcFormSummary').removeClass('d-none').text(msg);
                showAlert('danger', msg);
            }).always(() => { $btn.prop('disabled', false); $spinner.addClass('d-none'); });
        });

        // Delete: open confirm
        $(document).on('click', '.js-rc-delete', function () {
            pendingDeleteKey = $(this).data('key');
            $('#deleteRcKey').text(pendingDeleteKey || '');
            deleteModal && deleteModal.show();
        });

        $('#confirmDeleteRcBtn').on('click', function () {
            if (!pendingDeleteKey) return;
            const $btn = $(this); const $sp = $('#deleteRcSpinner');
            $btn.prop('disabled', true); $sp.removeClass('d-none');
            const token = $('#rcForm input[name="__RequestVerificationToken"]').val();
            $.post(urls.delete, { key: pendingDeleteKey, __RequestVerificationToken: token }).done(res => {
                if (typeof res === 'string' && res.includes('<!DOCTYPE')) {
                    showAlert('danger', 'انتهت الجلسة، يرجى تسجيل الدخول.');
                    return;
                }
                if (res.success) {
                    deleteModal && deleteModal.hide();
                    showAlert('success', res.message);
                    setTimeout(fullReload, 600);
                } else {
                    showAlert('danger', res.message || 'فشل الحذف.');
                }
            }).fail(xhr => {
                if (xhr.responseText && xhr.responseText.includes('<!DOCTYPE')) {
                    showAlert('danger', 'انتهت الجلسة، يرجى تسجيل الدخول.');
                    return;
                }
                const msg = xhr.responseJSON?.message || xhr.responseText?.substring(0, 500) || 'فشل الحذف.';
                showAlert('danger', msg);
            }).always(() => { $btn.prop('disabled', false); $sp.addClass('d-none'); });
        });

        // Publish — enhanced modal (replaces native confirm)
        const publishModalEl = document.getElementById('publishConfirmModal');
        const publishModal = publishModalEl ? bootstrap.Modal.getOrCreateInstance(publishModalEl) : null;
        const $confirmPublishBtn = $('#confirmPublishBtn');
        const $publishConfirmSpinner = $('#publishConfirmSpinner');

        function openPublishModal() {
            const isActivation = window.rcActivationSubId && window.rcActivationSubId !== '' && window.rcActivationSubId !== '00000000-0000-0000-0000-000000000000';
            const $iconWrap = $('#publishConfirmIcon');
            const $icon = $iconWrap.find('i');
            const $title = $('#publishConfirmLabel');
            const $subtitle = $('#publishConfirmSubtitle');
            const $body = $('#publishConfirmBody');
            const $confirmText = $('#confirmPublishText');

            if (isActivation) {
                $iconWrap.removeClass('bg-primary bg-opacity-10 bg-success bg-opacity-10').addClass('bg-success bg-opacity-10');
                $icon.removeClass().addClass('bi bi-lightning-charge-fill fs-4 text-success');
                $title.text('تأكيد النشر والتفعيل');
                $subtitle.text('سيتم نشر الإعدادات وتفعيل الاشتراك تلقائياً');
                $body.html(
                    '<div class="d-flex flex-column gap-2">' +
                    '  <div class="d-flex align-items-center gap-2"><span class="badge bg-success">is_active</span><code class="bg-light px-1 rounded">true</code><span class="text-body-secondary small">— تفعيل الاشتراك</span></div>' +
                    '  <div class="d-flex align-items-center gap-2"><span class="badge bg-light text-dark border">subscription_start_date</span><span class="font-monospace small">' + new Date().toISOString().slice(0,10) + '</span></div>' +
                    '  <div class="d-flex align-items-center gap-2"><span class="badge bg-light text-dark border">subscription_end_date</span><span class="text-body-secondary small">محسوب من الخطة</span></div>' +
                    '  <div class="alert alert-success py-2 mb-0 small"><i class="bi bi-check-circle me-1"></i> بعد النشر سيتغير حالة الاشتراك إلى <strong>نشط</strong> ويظهر للمستخدم.</div>' +
                    '</div>'
                );
                $confirmPublishBtn.removeClass('btn-primary btn-success').addClass('btn-success');
                $confirmText.text('نشر وتفعيل الآن');
            } else {
                const draftBadge = $('#publishBtn .badge').length ? ' (' + $('#publishBtn .badge').text().trim() + ')' : '';
                const paramCount = $('#rcTableContainer table tbody tr').length || 0;
                $iconWrap.removeClass('bg-success bg-opacity-10 bg-primary bg-opacity-10').addClass('bg-primary bg-opacity-10');
                $icon.removeClass().addClass('bi bi-cloud-upload fs-4 text-primary');
                $title.text('تأكيد نشر المسودة');
                $subtitle.text('سيتم دفع التغييرات إلى Firebase Remote Config' + draftBadge);
                $body.html(
                    '<ul class="mb-0 ps-3 small">' +
                    '  <li>عدد المفاتيح الحالية: <strong>' + paramCount + '</strong></li>' +
                    '  <li>التغييرات في المسودة ستصبح مباشرة بعد النشر</li>' +
                    '  <li class="text-body-secondary">لا يمكن التراجع إلا بنشر تعديل جديد</li>' +
                    '</ul>'
                );
                $confirmPublishBtn.removeClass('btn-success btn-primary').addClass('btn-primary');
                $confirmText.text('تأكيد النشر');
            }
            publishModal && publishModal.show();
        }

        function doPublish() {
            const $btn = $('#publishBtn');
            const origHtml = $btn.html();
            $confirmPublishBtn.prop('disabled', true);
            $publishConfirmSpinner.removeClass('d-none');
            $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-1"></span> جارِ النشر...');

            const token = $('#rcForm input[name="__RequestVerificationToken"]').val();
            $.post(urls.publish, { __RequestVerificationToken: token }).done(res => {
                publishModal && publishModal.hide();
                if (typeof res === 'string' && res.includes('<!DOCTYPE')) {
                    showAlert('danger', 'انتهت الجلسة، يرجى تسجيل الدخول.');
                    $btn.prop('disabled', false).html(origHtml);
                    return;
                }
                if (res.success) {
                    showAlert('success', res.message);
                    if (res.activated) {
                        setTimeout(() => {
                            const subId = res.subscriptionId || window.rcActivationSubId;
                            if (subId) window.location.href = '/Admin/Subscriptions/Details/' + subId;
                            else fullReload();
                        }, 900);
                    } else if (res.activationFailed) {
                        showAlert('warning', res.message);
                        $btn.prop('disabled', false).html(origHtml);
                    } else {
                        setTimeout(fullReload, 800);
                    }
                } else {
                    showAlert('danger', res.message || 'فشل النشر.');
                    $btn.prop('disabled', false).html(origHtml);
                }
            }).fail(xhr => {
                publishModal && publishModal.hide();
                if (xhr.responseText && xhr.responseText.includes('<!DOCTYPE')) {
                    showAlert('danger', 'انتهت الجلسة، يرجى تسجيل الدخول.');
                    $btn.prop('disabled', false).html(origHtml);
                    return;
                }
                const msg = xhr.responseJSON?.message || xhr.responseText?.substring(0, 500) || 'فشل النشر.';
                showAlert('danger', msg);
                $btn.prop('disabled', false).html(origHtml);
            }).always(() => {
                $confirmPublishBtn.prop('disabled', false);
                $publishConfirmSpinner.addClass('d-none');
            });
        }

        $('#publishBtn').on('click', openPublishModal);
        $confirmPublishBtn.on('click', doPublish);

        // Discard
        $('#discardBtn').on('click', function () {
            if (!confirm('تجاهل جميع تغييرات المسودة؟')) return;
            const $btn = $(this); $btn.prop('disabled', true);
            const token = $('#rcForm input[name="__RequestVerificationToken"]').val();
            $.post(urls.discard, { __RequestVerificationToken: token }).done(res => {
                if (typeof res === 'string' && res.includes('<!DOCTYPE')) {
                    showAlert('danger', 'انتهت الجلسة، يرجى تسجيل الدخول.');
                    return;
                }
                if (res.success) { showAlert('warning', res.message); setTimeout(fullReload, 600); }
                else showAlert('danger', res.message || 'فشل التجاهل.');
            }).fail(xhr => {
                if (xhr.responseText && xhr.responseText.includes('<!DOCTYPE')) {
                    showAlert('danger', 'انتهت الجلسة، يرجى تسجيل الدخول.');
                    return;
                }
                const msg = xhr.responseJSON?.message || xhr.responseText?.substring(0, 500) || 'فشل التجاهل.';
                showAlert('danger', msg);
            }).always(() => $btn.prop('disabled', false));
        });

        $('#refreshRcBtn').on('click', () => fullReload());

        rcModalEl && rcModalEl.addEventListener('hidden.bs.modal', () => {
            editingKey = null;
            $('#rcKey').prop('readonly', false);
        });
    });
})();
