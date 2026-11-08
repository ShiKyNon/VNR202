// Cuộn bản đồ để quân của mình nằm giữa khung nhìn.
window.htFollow = (smooth) => {
  const c = document.querySelector('.map-scroll');
  const p = document.querySelector('.piece.mine');
  if (!c || !p) return;
  const cr = c.getBoundingClientRect(), pr = p.getBoundingClientRect();
  c.scrollBy({
    top: pr.top + pr.height / 2 - (cr.top + cr.height * 0.36),
    left: pr.left + pr.width / 2 - (cr.left + cr.width / 2),
    behavior: smooth ? 'smooth' : 'auto'
  });
};
