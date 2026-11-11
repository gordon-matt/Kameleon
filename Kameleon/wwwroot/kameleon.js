// ThemeHead writes the first paint. After that, an open circuit calls applyDocument when the
// resolved theme changes. New link and script elements are created on purpose: writing a new
// href onto an element that is already in the document does not make the browser load it.
(function () {
    if (window.kameleon && window.kameleon.applyDocument) {
        return;
    }

    window.kameleon = window.kameleon || {};

    window.kameleon.applyDocument = function (stylesheets, variables, scripts) {
        var head = document.head;
        if (!head) {
            return;
        }

        var base = head.querySelector('link[data-kameleon="base"]');
        replaceGroup(head, 'link', 'stylesheet', stylesheets || [], function (url) {
            var link = document.createElement('link');
            link.rel = 'stylesheet';
            link.href = url;
            return link;
        }, base);

        var lastSheet = last(head, 'link[data-kameleon="stylesheet"]') || base;
        var style = syncVariables(head, variables || '', lastSheet);

        replaceGroup(head, 'script', 'script', scripts || [], function (url) {
            var script = document.createElement('script');
            script.src = url;
            script.defer = true;
            return script;
        }, style || lastSheet);
    };

    // Inserts urls, in order, immediately after `after`. An unchanged list is left alone so a
    // stylesheet or script that is already loaded is not fetched again.
    function replaceGroup(head, tag, kind, urls, create, after) {
        var nodes = head.querySelectorAll(tag + '[data-kameleon="' + kind + '"]');
        if (same(nodes, urls)) {
            return;
        }

        for (var i = 0; i < nodes.length; i++) {
            nodes[i].remove();
        }

        var pivot = after && after.parentNode === head ? after.nextSibling : null;
        for (var j = 0; j < urls.length; j++) {
            var element = create(urls[j]);
            element.setAttribute('data-kameleon', kind);
            head.insertBefore(element, pivot);
        }
    }

    function syncVariables(head, css, after) {
        var style = head.querySelector('style[data-kameleon="variables"]');
        if (!css) {
            if (style) {
                style.remove();
            }
            return null;
        }

        if (!style) {
            style = document.createElement('style');
            style.setAttribute('data-kameleon', 'variables');
        }

        if (style.textContent !== css) {
            style.textContent = css;
        }

        var pivot = after && after.parentNode === head ? after.nextSibling : null;
        if (pivot === style) {
            return style;
        }

        head.insertBefore(style, pivot);
        return style;
    }

    function last(head, selector) {
        var nodes = head.querySelectorAll(selector);
        return nodes.length ? nodes[nodes.length - 1] : null;
    }

    function same(nodes, urls) {
        if (nodes.length !== urls.length) {
            return false;
        }

        for (var i = 0; i < urls.length; i++) {
            var href = nodes[i].getAttribute('href') || nodes[i].getAttribute('src');
            if (href !== urls[i]) {
                return false;
            }
        }

        return true;
    }
})();
