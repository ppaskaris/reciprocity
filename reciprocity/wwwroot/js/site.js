(function (exports) {
    'use strict';

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

    function InvokeAutoSuggest(query, callback) {
        if (InvokeAutoSuggest.href == null) {
            throw new Error('InvokeAutoSuggest.href is not set.');
        }

        $.ajax({
            method: 'GET',
            url: InvokeAutoSuggest.href,
            data: {
                Query: query
            },
            dataType: 'json',
            success: function (results) {
                callback(results != null ? results : InvokeAutoSuggest.defaultResults);
            },
            failure: function () {
                alert('Error retrieving suggestions.');
                callback(InvokeAutoSuggest.defaultResults);
            }
        });
    }

    InvokeAutoSuggest.defaultResults = [];
    InvokeAutoSuggest.href = null;

    function MatchFunction() {
        return true;
    }

    function SortFunction(value) {
        return value;
    }

    function UpdateFunction(suggestion) {
        var copy = Object.assign({}, suggestion);
        copy.name = suggestion.value;
        return copy;
    }

    function HighlightWords(text) {
        var result = $('<div></div>').text(text);

        var query = this.query;
        var words = query.split(/[^a-z0-9]+/gi);
        if (words.length <= 0) {
            return result.html();
        }

        var searchValue = new RegExp('(?:' + words.join('|') + ')', 'gi');
        var highlighted = result.html().replace(searchValue, function (match) {
            return '<strong>' + match + '</strong>';
        });
        return result.html(highlighted).html();
    }

    function AcceptSuggestion(suggestion) {
        var $element = this.$element;
        $('#' + $element.attr('data-quantity-unit')).val(suggestion.servingUnit);
        $('#' + $element.attr('data-calories-per-serving')).val(suggestion.caloriesPerServing);
        $('#' + $element.attr('data-protein-per-serving')).val(suggestion.proteinPerServing);
        $('#' + $element.attr('data-serving')).val(suggestion.serving);
        $('#' + $element.attr('data-serving-unit')).val(suggestion.servingUnit);
        if (!suggestion.isTerminal) {
            this.lookup();
        }
    }

    function ConfigureAutoSuggest() {
        $(this).typeahead({
            source: InvokeAutoSuggest,
            items: 'all',
            minLength: 0,
            showHintOnFocus: true,
            matcher: MatchFunction,
            sorter: SortFunction,
            updater: UpdateFunction,
            highlighter: HighlightWords,
            autoSelect: false,
            afterSelect: AcceptSuggestion,
            delay: 150,
            skipHeadersAndDividers: true
        });
    }

    function InitAutoSuggest() {
        $('.js-AutoSuggest').each(ConfigureAutoSuggest);
    }

    function ConfigureScript() {
        var $config = $('meta[name="application-name"]');
        InvokeAutoSuggest.href = $config.attr('data-auto-suggest-href');
    }

    function HandleDocumentReady() {
        ConfigureScript();
        InitAutoSuggest();
        DoAutoFocus();
    }

    $(document).ready(HandleDocumentReady);

    // Exports.
    exports.ProxyLinkToButton = ProxyLinkToButton;
})(window);