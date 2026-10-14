# Contributing

Thanks for helping! Issues and pull requests are welcome in English or Portuguese.

```bash
git clone https://github.com/CaiqueDourado/PtBr.Text.git
cd PtBr.Text
dotnet test
```

Requirements: .NET 8 SDK or newer.

## The most useful contributions

- **A word that comes out wrong** (plural, gender, number in words): add it to the matching `InlineData` in the tests and fix the rule or the exception table. Cite a dictionary or grammar when the case is not obvious.
- **A phone, document or duration format** found in the wild: add the input to the tests.
- **Stopwords** that hurt or help keyword extraction on real texts.

## Guidelines

- No third-party dependencies in the library.
- Everything must behave the same with and without `InvariantGlobalization` (CI runs both). Do not use `string.Normalize` or culture-dependent formatting.
- Public members need XML documentation; the build treats warnings as errors.
- Test data must be invented: generate CPFs/CNPJs with `Cpf.Generate` / `Cnpj.Generate`, never use real people's documents.
