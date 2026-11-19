# Evidência: integração real com IA generativa (tradução de eventos)

Executado em 2026-11-17T01:00:39+00:00 (UTC).

## Configuração usada (por variável de ambiente; a chave NÃO é registrada aqui)
- Provedor: Google (Gemini), pelo endpoint compatível com OpenAI
- URL base: https://generativelanguage.googleapis.com/v1beta/openai
- Chamada: POST https://generativelanguage.googleapis.com/v1beta/openai/chat/completions  (Authorization: Bearer <chave omitida>)
- Modelo configurado: gemini-3.1-flash-lite
- Chave: 53 caracteres (valor omitido)

## Prompt enviado (somente metadados controlados; sem texto original, valor detectado ou exceção)
Explique para um cidadão leigo de 60 anos um evento do sistema de privacidade do Fácil Município. Dados do evento (controlados pelo sistema): tipo de dado = CPF; origem = texto de uma fonte administrativa; destino = gravação no banco de dados; gravidade potencial = Media; o que o sistema fez = o cadastro foi recusado porque o texto continha dado pessoal; nada foi gravado no banco de dados nem publicado. Regras: responda em português do Brasil, em no máximo duas frases, com linguagem simples; descreva fielmente o que o sistema fez, usando só as informações acima e sem inventar outros motivos; não afirme ataque, acesso indevido ou vazamento, pois nada disso foi comprovado; não mencione códigos técnicos; não peça nem repita dados pessoais.

## Resposta do provedor
- Status HTTP: 200
- Campo `model` devolvido pelo provedor: gemini-3.1-flash-lite
- Uso de tokens: {"completion_tokens": 39, "prompt_tokens": 169, "total_tokens": 208}
- Texto gerado:

> O sistema impediu a gravação do seu CPF no banco de dados porque identificou que o texto continha um dado pessoal sensível. Por segurança, nada foi registrado ou publicado no sistema.

## Onde isso aparece na aplicação
- Cada evento traduzido grava o modelo em `EventosSeguranca.TraducaoModelo` (ver 05-auditoria-sql.txt).
- O painel exibe "Explicação gerada por IA · modelo gemini-3.1-flash-lite" ao lado do texto (ver 03-eventos-com-explicacao-ia.png).
- Eventos sem tradução mostram "Mensagem padrão do sistema", nunca atribuída a um modelo.

## Limites
- O identificador do modelo e a URL base foram informados pelo integrante; a documentação oficial do provedor não foi consultada nesta sessão.
- A chave usada foi exposta em conversa durante a configuração e deve ser revogada.
