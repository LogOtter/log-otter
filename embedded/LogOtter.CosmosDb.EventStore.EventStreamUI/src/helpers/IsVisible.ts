export default function isVisible(element: HTMLElement) {
  const rect = element.getBoundingClientRect();

  const windowHeight =
    window.innerHeight || document.documentElement.clientHeight;

  const windowWidth = window.innerWidth || document.documentElement.clientWidth;

  return (
    rect.top >= 0 &&
    rect.left >= 0 &&
    rect.bottom <= windowHeight &&
    rect.right <= windowWidth
  );
}
