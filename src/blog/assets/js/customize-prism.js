Prism.languages.csharp["class-name"].push({
    pattern: /\s[A-Z]\w*\./,
    lookbehind: false,
    inside: { 'punctuation': /\./ }
});

Prism.hooks.add('complete', _ => {
    for(const e of document.getElementsByClassName("copy-to-clipboard-button"))
    {
        e.title = "Copy";
        e.ariaLabel = "Copy source code to clipboard";
    }
})
