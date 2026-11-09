# Registro de uso de IA

Registre usos relevantes de IA conforme o briefing da GS. Não inclua chaves, tokens, senhas, dados pessoais ou conteúdo privado. Resuma prompts longos quando necessário.

| Data | Integrante | Ferramenta/modelo | Objetivo e resumo do prompt | Resultado e revisão humana | Feature |
|---|---|---|---|---|---|
| 06/11/2026 | Matheus | Claude (Claude Code) | Analisar o briefing da GS e o planejamento do repositório; definir o plano da disciplina de Operating Systems; gerar os scripts Bash, units systemd e testes de falha da infraestrutura Linux. | Scripts executados e corrigidos pelo integrante numa VM Debian 13. Correções feitas durante os testes: leitura de CPU (`top`), locale pt-BR no `free`, reinício de serviços no `02-app.sh`, teste de memória. Resultados reais registrados na feature. | [Infraestrutura e IoT](docs/features/infraestrutura-e-iot.md) |
| 09/11/2026 | Equipe SOA/C# (revisão pelo integrante responsável pendente) | ChatGPT GPT-6 | Revisar o briefing CitizenTech, planejamento, arquitetura e contratos do repositório; implementar, corrigir e documentar API ASP.NET Core com EF Core/PostgreSQL, Neo4j, JWT, REST, Swagger, testes e modo determinístico explícito de demonstração. | Código proposto em branch para revisão humana; CI e integração devem ser verificados antes do merge. Nenhum dado pessoal real ou segredo incluído. | [SOA/C#](apps/api/README.md) |
