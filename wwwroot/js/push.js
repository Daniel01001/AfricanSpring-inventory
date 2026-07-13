// Registers the service worker and drives the "Order alerts" on/off toggle
// (checkbox id="pushToggle" on the More page) for this device.

(function () {
  const supported = 'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window;

  function b64ToUint8(base64) {
    const pad = '='.repeat((4 - (base64.length % 4)) % 4);
    const s = (base64 + pad).replace(/-/g, '+').replace(/_/g, '/');
    const raw = atob(s);
    return Uint8Array.from([...raw].map((c) => c.charCodeAt(0)));
  }

  async function subscribe() {
    const perm = await Notification.requestPermission();
    if (perm !== 'granted') {
      alert('Notifications are blocked. Turn them on for this site in your browser settings, then try again.');
      return false;
    }
    const reg = await navigator.serviceWorker.ready;
    const { publicKey } = await (await fetch('/push/publickey')).json();
    if (!publicKey) { alert('Push is not set up on the server yet.'); return false; }
    const sub = await reg.pushManager.subscribe({
      userVisibleOnly: true,
      applicationServerKey: b64ToUint8(publicKey)
    });
    const j = sub.toJSON();
    await fetch('/push/subscribe', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ endpoint: sub.endpoint, p256dh: j.keys.p256dh, auth: j.keys.auth })
    });
    return true;
  }

  async function unsubscribe() {
    const reg = await navigator.serviceWorker.ready;
    const sub = await reg.pushManager.getSubscription();
    if (sub) {
      await fetch('/push/unsubscribe', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ endpoint: sub.endpoint })
      });
      await sub.unsubscribe();
    }
  }

  document.addEventListener('DOMContentLoaded', async () => {
    if (supported) {
      try { await navigator.serviceWorker.register('/service-worker.js'); }
      catch (e) { console.warn('SW register failed', e); }
    }

    const toggle = document.getElementById('pushToggle');
    if (!toggle) return;

    if (!supported) { toggle.disabled = true; return; }

    // Reflect the current subscription state on this device.
    try {
      const reg = await navigator.serviceWorker.ready;
      const sub = await reg.pushManager.getSubscription();
      toggle.checked = !!sub && Notification.permission === 'granted';
    } catch (e) { /* leave unchecked */ }

    toggle.addEventListener('change', async () => {
      const wanted = toggle.checked;
      toggle.disabled = true;
      try {
        if (wanted) {
          toggle.checked = await subscribe();
        } else {
          await unsubscribe();
          toggle.checked = false;
        }
      } catch (e) {
        console.error('push toggle failed', e);
        alert('Could not change notifications: ' + (e && e.message ? e.message : e));
        toggle.checked = !wanted;
      } finally {
        toggle.disabled = false;
      }
    });
  });
})();
