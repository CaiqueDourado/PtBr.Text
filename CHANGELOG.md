# Changelog

All notable changes to this project are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses [Semantic Versioning](https://semver.org/).

## [0.1.0] - 2026-10-14

### Added

- Documents: `Cpf`, `Cnpj` (numeric and the new alphanumeric format), `Cep` with state lookup, `Phone` (mobile, landline, 0800/0300, 400x, +55, trunk and carrier prefixes).
- `TextNormalizer`: ICU-free accent removal, slugs, whitespace, Portuguese title case.
- `NumberToWords` (cardinals with gender, reais), `DurationParser` (Portuguese → ISO 8601), `BrazilianFormat` (culture-independent money, numbers and dates).
- `Inflector` (plural, plural detection, gender guess) and `SeoTitles` ("Qual a melhor geladeira?").
- `PriceHistoryNarrator`: sentences and FAQ entries from a price history.
- `ContactExtractor`: phones, e-mails and URLs in free text, with `Linkify`.
- `RakeExtractor` and Portuguese `Stopwords`.
