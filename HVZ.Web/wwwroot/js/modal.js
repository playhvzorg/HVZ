
function showModalById(modalId) {
    const modal = new bootstrap.Modal(document.getElementById(modalId));

    if (modal === undefined) {
        console.error("Could not find modal #" + modalId);
    }

    modal.show();
}

function hideModalById(modalId) {
    const modal = new boostrap.Modal(document.getElementById(modalId));

    if (modal === undefined) {
        console.error("Could not find modal #" + modalId);
    }

    modal.hide();
}
