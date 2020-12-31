Prism.languages.csharp["class-name"].push({
    pattern: /\s[A-Z]\w*\./,
    lookbehind: false,
    inside: { 'punctuation': /\./ }
});