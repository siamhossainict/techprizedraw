/**
 * TECH Prize Draw - Client-Side Interactive Engine
 */

document.addEventListener('DOMContentLoaded', () => {
    initLiveInventoryUpdater();
    initCopyButtons();
    initPaymentMethodCards();
    initTransactionIdUppercase();
    initFloatingScrollButton();
});

/**
 * Periodically polls the server-side single source of truth for ticket inventory.
 * 20-second interval balances freshness with server resource economy.
 */
function initLiveInventoryUpdater() {
    const remainingEl = document.querySelector('.live-remaining-count');
    const approvedEl = document.querySelector('.live-approved-count');
    const pendingEl = document.querySelector('.live-pending-count');
    const progressFill = document.querySelector('.live-progress-fill');
    const progressPending = document.querySelector('.live-progress-pending');

    if (!remainingEl) return;

    async function fetchInventory() {
        try {
            const response = await fetch('/api/campaign/inventory', {
                headers: { 'Accept': 'application/json' }
            });
            if (!response.ok) return;

            const data = await response.json();

            if (remainingEl) {
                remainingEl.textContent = Number(data.remaining).toLocaleString();
            }
            if (approvedEl) {
                approvedEl.textContent = Number(data.approved).toLocaleString();
            }
            if (pendingEl) {
                pendingEl.textContent = Number(data.pending).toLocaleString();
            }

            const total = data.total || 5000;
            const approvedPercent = (data.approved / total) * 100;
            const pendingPercent = (data.pending / total) * 100;

            if (progressFill) {
                progressFill.style.width = `${approvedPercent}%`;
            }
            if (progressPending) {
                progressPending.style.left = `${approvedPercent}%`;
                progressPending.style.width = `${pendingPercent}%`;
            }
        } catch (err) {
            console.debug('Inventory refresh notice:', err);
        }
    }

    // Refresh every 20 seconds
    setInterval(fetchInventory, 20000);
}

/**
 * Copy to clipboard with instant feedback & toast message
 */
function initCopyButtons() {
    document.querySelectorAll('.btn-copy').forEach(btn => {
        btn.addEventListener('click', async (e) => {
            e.preventDefault();
            const textToCopy = btn.getAttribute('data-copy');
            if (!textToCopy) return;

            try {
                await navigator.clipboard.writeText(textToCopy);
                const originalText = btn.innerHTML;
                btn.innerHTML = '✓ Copied!';
                btn.classList.add('copied');

                showTechToast(`Copied ${textToCopy} to clipboard!`);

                setTimeout(() => {
                    btn.innerHTML = originalText;
                    btn.classList.remove('copied');
                }, 2200);
            } catch (err) {
                // Fallback for older browsers
                const tempInput = document.createElement('input');
                tempInput.value = textToCopy;
                document.body.appendChild(tempInput);
                tempInput.select();
                document.execCommand('copy');
                document.body.removeChild(tempInput);

                showTechToast(`Copied ${textToCopy} to clipboard!`);
            }
        });
    });
}

/**
 * Interactive card selection for checkout payment methods
 */
function initPaymentMethodCards() {
    const cards = document.querySelectorAll('.payment-method-card');
    cards.forEach(card => {
        card.addEventListener('click', () => {
            cards.forEach(c => c.classList.remove('selected'));
            card.classList.add('selected');

            const radio = card.querySelector('input[type="radio"]');
            if (radio) {
                radio.checked = true;
                radio.dispatchEvent(new Event('change', { bubbles: true }));
            }
        });
    });
}

/**
 * Ensure transaction IDs are consistently formatted
 */
function initTransactionIdUppercase() {
    const txInput = document.querySelector('#TransactionId');
    if (txInput) {
        txInput.addEventListener('input', () => {
            txInput.value = txInput.value.toUpperCase().replace(/\s+/g, '');
        });
    }
}

/**
 * Premium toast notification helper
 */
function showTechToast(message) {
    let container = document.querySelector('.toast-tech-container');
    if (!container) {
        container = document.createElement('div');
        container.className = 'toast-tech-container';
        document.body.appendChild(container);
    }

    const toast = document.createElement('div');
    toast.className = 'toast-tech';
    toast.innerHTML = `<span>⚡</span> <span>${message}</span>`;
    container.appendChild(toast);

    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transition = 'opacity 0.4s ease';
        setTimeout(() => toast.remove(), 400);
    }, 2800);
}

/**
 * Floating Quick Scroll Button (Scroll to Bottom / Scroll to Top)
 */
function initFloatingScrollButton() {
    const btn = document.getElementById('floatingScrollBtn');
    if (!btn) return;
    const icon = document.getElementById('scrollBtnIcon');
    const text = document.getElementById('scrollBtnText');

    function updateButton() {
        const scrollTotal = document.documentElement.scrollHeight - window.innerHeight;
        const currentScroll = window.scrollY;

        // If user is more than 35% down, flip button to Scroll to Top
        if (currentScroll > 500) {
            btn.title = 'Scroll back to top';
            if (icon) icon.textContent = '↑';
            if (text) text.textContent = 'Back to Top';
            btn.onclick = () => window.scrollTo({ top: 0, behavior: 'smooth' });
        } else {
            btn.title = 'Scroll to bottom / footer';
            if (icon) icon.textContent = '↓';
            if (text) text.textContent = 'Scroll to Bottom';
            btn.onclick = () => window.scrollTo({ top: document.documentElement.scrollHeight, behavior: 'smooth' });
        }
    }

    window.addEventListener('scroll', updateButton, { passive: true });
    updateButton();
}
