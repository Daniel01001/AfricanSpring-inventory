// Registers the service worker and lets an app user turn on order push alerts
// on the current device. A button with id="enablePush" wires itself up.

(function () {
  const supported = 'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window;

  function b64ToUint8(base64) {
    const pad = '='.repeat((4 - (base64.length % 4)) % 4);
    const s = (base64 + pad).replace(/-/g, '+').replace(/_/g, '/');
    const raw = atob(s);
    return Uint8Array.from([...raw].map((c) => c.charCodeAt(0)));
  }

  function setBtn(btn, text, disabled) {
    if (!btn) return;
    btn.textContent = text;
    btn.disabled = !!disabled;
  }

  async function enable(btn) {
    try {
      if (!supported) {
        alert('Notifications are not supported here. On iPhone/iPad, tap Share → "Add to Home Screen", open the app from that icon, then try again.');
        return;
      }
      const perm = await Notification.requestPermission();
      if (perm !== 'granted') {
        alert('Notifications are blocked. Turn them on for this site in your browser settings, then try again.');
        return;
      }
      const reg = await navigator.serviceWorker.ready;
      const res = await fetch('/push/publickey');
      const { publicKey } = await res.json();
      if (!publicKey) {
        alert('Push is not set up on the server yet.');
        return;
      }
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
      setBtn(btn, 'Order alerts are on ✓', true);
    } catch (e) {
      console.error('enable push failed', e);
      alert('Could not turn on notifications: ' + (e && e.message ? e.message : e));
    }
  }

  document.addEventListener('DOMContentLoaded', async () => {
    if (!supported) return;
    let reg;
    try {
      reg = await navigator.serviceWorker.register('/service-worker.js');
    } catch (e) {
      console.warn('SW register failed', e);
    }

    const btn = document.getElementById('enablePush');
    if (!btn) return;

    // Reflect current state: only "on" if we actually hold a live subscription.
    try {
      const ready = await navigator.serviceWorker.ready;
      const existing = await ready.pushManager.getSubscription();
      if (existing && Notification.permission === 'granted') {
        setBtn(btn, 'Order alerts are on ✓', true);
      }
    } catch (e) { /* leave button as-is */ }

    btn.addEventListener('click', () => enable(btn));
  });
})();
