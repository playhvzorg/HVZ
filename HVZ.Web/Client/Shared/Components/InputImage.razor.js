// https://learn.microsoft.com/en-us/aspnet/core/blazor/file-uploads?view=aspnetcore-7.0&pivots=webassembly

/**
 * @param {Uint8Array} data
 * @param {string} name
 * @param {string} type contentType string
 * */
export function createObjectURL(data, name, type) {
    const file = new File([data], name, { type: type });
    const url = URL.createObjectURL(file)
    const img = document.getElementById("preview-img");
    img.addEventListener('load', () => URL.revokeObjectURL(url), { once: true });
    return url;
}