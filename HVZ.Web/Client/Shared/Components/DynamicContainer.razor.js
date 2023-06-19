
// TODO add an option for footer content for a container
/**
 * Initialize a container that extends to the bottom of the screen and updates with the screen size
 * @param {HTMLElement} elem Container element
 * @param {number} bottomMargin Desired distance from the bottom of the screen
 */
export function initializeContainer(elem, bottomMargin) {
    // const y = elem.getBoundingClientRect().top;
    // const height = window.innerHeight - y - elem.offsetTop - bottomMargin;

    // elem.style.height = `${height}px`;
    setElemSize(elem, bottomMargin);
    window.addEventListener('resize', () => {
        
        setElemSize(elem, bottomMargin);
    });
}

function setElemSize(elem, bottomMargin) {
    const y = elem.getBoundingClientRect().top;
    const elemHeight = window.innerHeight - y - elem.offsetTop - bottomMargin;
    elem.style.height = `${elemHeight}px`;
}