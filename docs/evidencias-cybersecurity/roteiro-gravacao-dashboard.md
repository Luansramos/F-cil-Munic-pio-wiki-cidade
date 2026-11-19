# Roteiro de gravação — dashboard ao vivo (≈ 60 s)

Objetivo: mostrar o **painel funcionando de verdade** (o vídeo atual só tem miniaturas). Gravar a tela do navegador e inserir este trecho no vídeo, por exemplo depois do slide "Registrar somente a decisão" ou no lugar dos slides de captura.

## Antes de gravar
1. API, PostgreSQL e Neo4j no ar. A tradução por IA precisa estar configurada (`RiskTranslator__*`) para a etapa 5.
2. Chrome em janela de **1280×720 ou maior**, zoom de **125 %** (o texto do painel é pequeno em vídeo). Feche outras abas e notificações.
3. Dois terminais PowerShell prontos, **sem** mostrar variáveis de ambiente, chaves ou histórico de comandos com a chave.
4. Gravador: Xbox Game Bar (`Win + Alt + R`, grava a janela ativa) ou OBS. Grave só a janela do navegador (e, na etapa 4, o terminal).
5. Use somente dados simulados. Abra `http://127.0.0.1:5055/auditoria/` (use `127.0.0.1`, não `localhost`).

## Sequência (tempos aproximados)

| Tempo | Na tela | Fala sugerida |
|---|---|---|
| 0–8 s | Página de login vazia | "O painel é uma página estática. Antes do login ela não contém nenhum dado." |
| 8–15 s | Entrar como operador (`operador@example.test`) | "O login reutiliza a autenticação da API. O token fica só na memória desta aba." |
| 15–28 s | Município Poá, topo do painel: linha **Sessão: perfil OPERADOR**, farol **AMARELO — Monitorar**, contagens | "O Risco Cidadão vem de regras do código. Amarelo significa dado pessoal detectado e tratado. Bloqueio bem-sucedido não é vazamento confirmado." |
| 28–40 s | Rolar até **Eventos recentes**; **dar zoom** em uma linha: Origem, Destino, Ação, Gravidade e a etiqueta **"Explicação gerada por IA · modelo gemini-3.1-flash-lite"** | "Cada evento mostra categoria, origem, destino e ação. Não há CPF nem texto original: só metadados. A explicação foi gerada pelo Gemini, e o modelo fica registrado no evento." |
| 40–52 s | **Evento novo ao vivo** (comando abaixo) e clique em **Atualizar** | "Vou enviar uma pergunta com um RG simulado. Ela é sanitizada antes de seguir." |
| 52–60 s | Atualizar de novo após ~10 s: a linha nova passa de **"Mensagem padrão do sistema (tradução por IA pendente)"** para **"Explicação gerada por IA"** | "Enquanto a IA não responde, o painel mostra uma mensagem padrão identificada como tal. Quando a tradução chega, aparece a etiqueta da IA com o modelo." |

Opcional (+10 s): clicar em **Sair**, entrar como ADMIN do município e mostrar **Sessão: perfil ADMIN · município poa-demo-…** com um único município na lista.

## Comando da etapa 5 (evento novo ao vivo)
Ele cria um evento de categoria **RG** na pergunta, combinação ainda não traduzida, o que força uma chamada real ao provedor. Rode no segundo terminal:

```powershell
$id="poa-demo-b21f845128"
$body = @{pergunta="Meu RG é 12.345.678-9. Como funciona o Cata Treco?"} | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "http://127.0.0.1:5055/api/v1/municipios/$id/conversas" -ContentType "application/json; charset=utf-8" -Body ([Text.Encoding]::UTF8.GetBytes($body)) | Select-Object informacaoSuficiente
```
- Se a combinação já tiver sido traduzida em outra gravação, a nova linha reaproveita o texto e vira "gerada por IA" quase imediatamente. Para forçar uma chamada nova, troque o dado por outra categoria ainda não usada nessa origem, por exemplo `Minha senha é Abc12345`.
- A tradução ocorre no ciclo do worker (5 s com `RiskTranslator__PollSeconds=5`); espere 10–15 s antes do último "Atualizar".

## O que não mostrar nem dizer
- Não mostre a chave do provedor, variáveis de ambiente ou o terminal onde a API foi iniciada.
- Não diga que a consulta municipal usa uma LLM: ela está em modo demonstração (`Llm__Mode=demo`) e apenas repete texto já revisado. Só a **tradução dos eventos** usa IA real (Gemini).
- Não afirme que o painel "prova" que não houve vazamento: sem eventos não é garantia, e a proteção cobre só os caminhos instrumentados.
- Bloqueios de **resposta pública** e de **grafo** não são reproduzíveis ao vivo (exigiriam uma IA gerando dado pessoal); cite que são cobertos por testes automatizados.

## Conferência rápida da gravação
- A linha **Sessão: perfil …** está visível em algum momento.
- A etiqueta com o **nome do modelo** está legível (zoom).
- Aparece pelo menos um evento com **Ação: Bloqueado** (os dois de persistência).
- O texto do evento ao vivo muda de "mensagem padrão" para "gerada por IA", ou você diz explicitamente que ainda está pendente.
