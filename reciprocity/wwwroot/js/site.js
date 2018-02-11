// Bootstrap dropdowns only support links, so proxy the link to the correct
// button.
function ProxyLinkToButton(event, id) {
    event.preventDefault();
    var element = document.getElementById(id);
    if (element != null) {
        element.click();
    }
}

// Work around for flash of unstyled content (FOUC) in Firefox when using the
// "autofocus" attribute.
function DoAutoFocus() {
    $('.js-autofocus').first().focus();
}

function HandleDocumentReady() {
    DoAutoFocus();
}

$(document).ready(HandleDocumentReady);