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
    $('.js-AutoFocus').first().focus();
}

function DoAutoSuggest($input, query) {
    $input.removeData('AutoSuggestTimeout');

    var $dataList = $('#' + $input.attr('list'));
    $.ajax({
        method: 'GET',
        url: $input.attr('data-href'),
        data: {
            Query: query
        },
        dataType: 'html',
        success: function (fragment) {
            var lastQuery = $input.data('AutoSuggestLastQuery');
            if (lastQuery !== query) return;
            if (fragment != null) {
                $dataList.html(fragment);
            } else {
                $dataList.empty();
            }
        },
        failure: function () {
            var lastQuery = $input.data('AutoSuggestLastQuery');
            if (lastQuery !== query) return;
            $input.data('AutoSuggestVersion', 0);
            $dataList.empty();
        }
    });
}

function AbortAutoSuggest($input) {
    var timeout = $input.data('AutoSuggestTimeout');
    if (timeout != null) {
        clearTimeout(timeout);
        $input.removeData('AutoSuggestTimeout')
    }
}

function QueueAutoSuggest(event) {
    // Debounce so as to not send *so* many requests.
    var $input = $(this);
    var query = $input.val();
    var lastQuery = $input.data('AutoSuggestLastQuery');
    if (query !== lastQuery) {
        AbortAutoSuggest($input);
        var timeout = setTimeout(DoAutoSuggest, 250, $input, query);
        $input.data('AutoSuggestTimeout', timeout);
        $input.data('AutoSuggestLastQuery', query)
    }
}

function AcceptAutoSuggest() {
    // If they selected a suggestion, be helpful and fill in the corresponding
    // serving form fields.
    var $input = $(this);
    var value = $input.val();
    var $dataList = $('#' + $input.attr('list'));
    $dataList.find('option').each(function (_, option) {
        var $option = $(option);
        var didSelectOption = $option.attr('value') === value;
        if (didSelectOption) {
            $input.val($option.attr('data-value'));
            $('#' + $input.attr('data-quantity-unit')).val($option.attr('data-serving-unit'));
            $('#' + $input.attr('data-calories-per-serving')).val($option.attr('data-calories-per-serving'));
            $('#' + $input.attr('data-serving')).val($option.attr('data-serving'));
            $('#' + $input.attr('data-serving-unit')).val($option.attr('data-serving-unit'));
            if ($option.attr('data-is-terminal') === "True") {
                AbortAutoSuggest($input);
            }
            return false;
        }
    });
}

function InitAutoSuggest() {
    $(document).on('keyup input', '.js-AutoSuggest', QueueAutoSuggest);
    $(document).on('input', '.js-AutoSuggest', AcceptAutoSuggest);
}

function HandleDocumentReady() {
    DoAutoFocus();
    InitAutoSuggest();
}

$(document).ready(HandleDocumentReady);