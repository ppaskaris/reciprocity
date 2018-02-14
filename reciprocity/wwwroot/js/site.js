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

function DoAutoSuggest($input) {
    $input.removeData('AutoSuggestTimeout');

    // Version fixes concurrency issues where requests interleave with high
    // latency.We mark ourselves as the "current version" and abort if someone
    // else has claimed that title by the time our callback resolves.
    var myVersion = $input.data('AutoSuggestVersion');
    if (myVersion == null) myVersion = 0;
    $input.data('AutoSuggestVersion', myVersion += 1);

    var $dataList = $('#' + $input.attr('list'));
    $.ajax({
        method: 'GET',
        url: $input.attr('data-AutoSuggest'),
        data: {
            Query: $input.val()
        },
        dataType: 'html',
        success: function (fragment) {
            var version = $input.data('AutoSuggestVersion');
            if (version !== myVersion) return;
            if (fragment != null) {
                $dataList.html(fragment);
            } else {
                $dataList.empty();
            }
        },
        failure: function () {
            var version = $input.data('AutoSuggestVersion');
            if (version !== myVersion) return;
            $dataList.empty();
        }
    });
}

function QueueAutoSuggest(event) {
    // Only alphabet characters should trigger auto-complete.
    if (event.keyCode < 65 || event.keyCode > 90) {
        return;
    }

    // Debounce so as to not send *so* many requests.
    var $input = $(this);
    var timeout = $input.data('AutoSuggestTimeout');
    if (timeout >= 0) {
        clearTimeout(timeout);
    }
    timeout = setTimeout(DoAutoSuggest, 50, $input);
    $input.data('AutoSuggestTimeout', timeout);
}

function AcceptAutoSuggest() {
    var $input = $(this);
    var timeout = $input.data('AutoSuggestTimeout');
    if (timeout >= 0) {
        clearTimeout(timeout);
        $input.removeData('AutoSuggestTimeout');
    }

    // If they selected a suggestion, be helpful and fill in the corresponding
    // serving form fields.
    var value = $input.val();
    var $dataList = $('#' + $input.attr('list'));
    $dataList.find('option').each(function (_, option) {
        var $option = $(option);
        var didSelectOption = $option.attr('value') === value;
        if (didSelectOption) {
            $('#' + $input.attr('data-CaloriesPerServing')).val($option.attr('data-CaloriesPerServing'));
            $('#' + $input.attr('data-Serving')).val($option.attr('data-Serving'));
            $('#' + $input.attr('data-ServingUnit')).val($option.attr('data-ServingUnit'));
            return false;
        }
    });
}

function InitAutoSuggest() {
    $(document).on('keyup', '.js-AutoSuggest', QueueAutoSuggest);
    $(document).on('input', '.js-AutoSuggest', AcceptAutoSuggest);
}

function HandleDocumentReady() {
    DoAutoFocus();
    InitAutoSuggest();
}

$(document).ready(HandleDocumentReady);