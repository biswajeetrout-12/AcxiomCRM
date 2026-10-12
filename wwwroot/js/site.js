/* AcxiomCRM — Site JavaScript */

// ── Live clock ────────────────────────────────────────────────
function updateClock() {
    const el = document.getElementById('live-time');
    if (el) {
        const now = new Date();
        const options = { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit', hour12: false };
        el.textContent = now.toLocaleDateString('en-GB', options).replace(',', '');
    }
}
setInterval(updateClock, 30000);

// ── Mobile Sidebar ────────────────────────────────────────────
function openSidebar() {
    const sidebar  = document.getElementById('crmSidebar');
    const overlay  = document.getElementById('sidebarOverlay');
    if (sidebar) sidebar.classList.add('open');
    if (overlay) overlay.style.setProperty('display', 'block', 'important');
}

function closeSidebar() {
    const sidebar  = document.getElementById('crmSidebar');
    const overlay  = document.getElementById('sidebarOverlay');
    if (sidebar) sidebar.classList.remove('open');
    if (overlay) overlay.style.removeProperty('display');
}

document.addEventListener('DOMContentLoaded', function () {
    // Toggle sidebar on mobile
    const toggleBtn = document.getElementById('sidebarToggle');
    if (toggleBtn) {
        toggleBtn.addEventListener('click', openSidebar);
    }

    // Auto-dismiss toasts after 4s
    setTimeout(function () {
        const toasts = document.querySelectorAll('.crm-toast');
        toasts.forEach(function (toast) {
            toast.style.transition = 'opacity 0.5s ease, transform 0.5s ease';
            toast.style.opacity = '0';
            toast.style.transform = 'translateX(20px)';
            setTimeout(function () { toast.remove(); }, 500);
        });
    }, 4000);

    // Confirm dialogs on delete forms (backup for inline onsubmit)
    document.querySelectorAll('form[data-confirm]').forEach(function (form) {
        form.addEventListener('submit', function (e) {
            const message = form.dataset.confirm || 'Are you sure you want to perform this action?';
            if (!confirm(message)) {
                e.preventDefault();
            }
        });
    });

    // Add loading spinner to submit buttons on form submit
    document.querySelectorAll('form:not([data-no-loading])').forEach(function (form) {
        form.addEventListener('submit', function () {
            const btn = form.querySelector('button[type="submit"]:not(.no-loading)');
            if (btn && !btn.classList.contains('btn-outline-danger')) {
                const original = btn.innerHTML;
                btn.disabled = true;
                btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status"></span>Processing...';
                // Restore after 5s as failsafe
                setTimeout(function () {
                    btn.disabled = false;
                    btn.innerHTML = original;
                }, 5000);
            }
        });
    });

    // Table row highlighting
    document.querySelectorAll('.table-hover tbody tr').forEach(function (row) {
        row.style.cursor = 'pointer';
        const link = row.querySelector('a[href]');
        if (link) {
            row.addEventListener('click', function (e) {
                if (!e.target.closest('a') && !e.target.closest('button') && !e.target.closest('form')) {
                    window.location.href = link.href;
                }
            });
        }
    });

    // Tooltip initialization
    const tooltipEls = document.querySelectorAll('[data-bs-toggle="tooltip"]');
    tooltipEls.forEach(function (el) {
        new bootstrap.Tooltip(el, { trigger: 'hover' });
    });
});
