
function hasOverflow(elementId) {
    let element = document.getElementById(elementId);
    if (element == undefined) {
        console.error(`Could not find target element with ID: ${elementId}`);
    }

    return element.scrollHeight > element.offsetHeight;
}

function getScrollHeight(elementId) {
    let element = document.getElementById(elementId);
    if (element == undefined) {
        console.error(`Could not find target element with ID: ${elementId}`);
    }

    return element.scrollHeight;
}

function getOffsetHeight(elementId) {
    let element = document.getElementById(elementId);
    if (element == undefined) {
        console.error(`Could not find target element with ID: ${elementId}`);
    }

    return element.offsetHeight;
}