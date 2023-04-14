
function showModalById(modalId) {
    const modal = new bootstrap.Modal(document.getElementById(modalId));

    if (modal === undefined) {
        console.error("Could not find modal #" + modalId);
    }

    modal.show();
}

function hideModalById(modalId) {
    const modal = new bootstrap.Modal(document.getElementById(modalId));

    if (modal === undefined) {
        console.error("Could not find modal #" + modalId);
    }

    modal.hide();
}

function toggleModalById(modalId) {
    const modal = new bootstrap.Modal(document.getElementById(modalId));

    if (modal === undefined) {
        console.error("Could not find modal #" + modalId);
    }

    modal.toggle();
}

async function swapModals(modalId1, modalId2) {
    const current = new bootstrap.Modal(document.getElementById(modalId1));
    const target = new bootstrap.Modal(document.getElementById(modal2));

    if (current === undefined) {
        console.error("Could not find modal #" + modalId1);
    }

    if (target === undefined) {
        console.error("Could not find modal #" + modalId2);
    }

    await current.toggle();
    await target.show();
}
