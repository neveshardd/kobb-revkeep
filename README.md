# KOBB Revkeep

Plugin de backups automáticos para Revit 2025 e 2026. Código e assets ficam em `softwares/kobb-revkeep`; `products` é reservado aos instaláveis.

Na aba KOBB Studio, Revkeep abre a configuração do projeto atual. O nome e as duas pastas são editáveis. Concluir salva a configuração por usuário e projeto; não executa uma operação manual de backup.

Antes do salvamento, preserva o RVT que estava no disco. Depois de um salvamento bem-sucedido, preserva o novo RVT. O plugin não chama Save ou Synchronize no projeto aberto. As versões usam pastas únicas com data UTC, etapa e identificador; não há exclusão automática.

No próximo período disponível do Revit, exporta as famílias carregadas que a API permite editar. A exportação corresponde ao estado disponível nesse momento, não a um estado histórico anterior ao salvamento. Famílias de sistema e não exportáveis permanecem no RVT; o relatório registra as famílias não exportadas e eventuais falhas. O Revit pode ficar ocupado durante a exportação de projetos grandes.

Depois do salvamento, transfere somente os arquivos numerados do próprio projeto, como `Projeto.0001.rvt`, para `backups-revit` na pasta da versão. Verifica SHA-256 da cópia antes de remover a origem. Arquivos de outros projetos permanecem intactos. Não transfere pastas de backups de modelos centrais.

Esta primeira implementação atende RVTs locais sem compartilhamento. Modelos centrais, Revit Server e Autodesk Docs precisam de um fluxo próprio antes de serem habilitados. Salvar como outro arquivo cria uma nova identidade de projeto e exige configuração desse novo caminho.

Relatórios ficam em `backup.json` dentro de cada versão. Falhas ficam em `%LOCALAPPDATA%/KOBB/Revkeep/errors.log`. Se o documento fechar antes da exportação, o RVT preservado permanece e o relatório informa que as famílias não foram exportadas.

## Desenvolvimento

`src/Kobb.Revkeep.Core` contém configurações e transferências verificadas. `src/Kobb.Revkeep.UI` contém a interface WPF. `src/Kobb.Revkeep` integra eventos e famílias à API Autodesk. `src/Kobb.Revkeep.Preview` permite revisar a interface sem Revit.

Compilar: `./scripts/Build.ps1 -RevitVersion 2026`. Para 2025, fornecer as DLLs oficiais em `-RevitApiDirectory`. O script identifica o runtime pelo arquivo oficial `RevitAPI.runtimeconfig.json`; sem esse arquivo, informar `-TargetFramework net8.0-windows` ou `net10.0-windows`. As atualizações 2025.5 e 2026.5 usam .NET 10; versões anteriores usam .NET 8. Cada versão deve ser compilada e validada contra sua própria API. Nenhuma DLL Autodesk é redistribuída.

Prévia: `dotnet run --project src/Kobb.Revkeep.Preview`. Testes: `dotnet run --project tests/Kobb.Revkeep.Tests`.

O instalador compartilhado permanece em `softwares/kobb-installer`. O catálogo deste plugin está em `product.json`. `scripts/Package-Installer.ps1` compila o plugin, prepara o inventário SHA-256 e gera `products/kobb-revkeep/kobb-revkeep-setup-<versão>.exe` e o ZIP completo. O pacote 0.1.4 é uma versão de testes para Revit 2026.5 com .NET 10; o instalador recusa instalações que utilizem outro runtime. Mantenha o executável, `product.json` e `payload` juntos; para transferir o instalador, use o ZIP.

## Validação atual

O instalador de plugins não oferece atalhos na área de trabalho ou fixação na barra de tarefas. Fechar antes da conclusão pede confirmação com o visual KOBB; durante a instalação real, o fechamento permanece bloqueado para preservar a operação.

Para instalar, feche todos os projetos abertos; o Revit pode permanecer aberto. Quando o Revit está em execução, o instalador pede confirmação de que os projetos foram fechados. Essa confirmação é do usuário; não representa uma consulta automática aos documentos do Revit.

A interface e a integração com as DLLs oficiais do Revit 2026.5 compilam sem avisos. Os testes de preservação e transferência de arquivos e os testes da interface foram aprovados. A execução dentro do Revit, as transferências reais e as builds de Revit 2025 e versões anteriores a 2026.5 ainda precisam de validação.

O ícone da faixa do Revit preserva a resolução do asset original e é dimensionado durante a exibição. A tela de carregamento anima o círculo independentemente do progresso dos arquivos durante o mínimo de 15 segundos. Os testes de ícones, animação, atualização e rollback foram aprovados. Atualizações preservam a pasta da versão anterior e restauram o registro anterior em caso de falha; se o plugin já estiver carregado, reabra o Revit para carregar a nova DLL.

Ao abrir um projeto RVT local sem configuração válida, a janela de configuração aparece automaticamente no próximo momento disponível do Revit. Cancelar não repete o popup na mesma abertura. Reabrir o projeto ou salvar como um novo caminho permite uma nova configuração. Projetos já configurados não abrem o popup. Modelos não suportados por esta versão permanecem fora desse fluxo.

Os campos têm cursor de texto, caret preto, seleção visível, foco por teclado e nomes para tecnologias assistivas. Atalhos: Alt+N para nome, Alt+P para pasta do projeto, Alt+F para pasta das famílias, Alt+B e Alt+M para selecionar pastas. Tab percorre os controles; Enter conclui e Esc cancela. A barra fica no topo, com botões transparentes fora do hover e janela sem arredondamento.

A configuração identifica cada campo com um rótulo visível. Ao concluir, cria a estrutura Backup-NomeDoProjeto/backups e Backup-NomeDoProjeto/familias nas pastas base escolhidas. Com a mesma pasta base nos dois campos, ambas as subpastas ficam sob o mesmo diretório do projeto. As versões datadas ficam dentro dessas subpastas. As pastas e os backups anteriores são preservados.

As janelas de configuração, instalação, carregamento e confirmação têm borda cinza de 1 DIP, sobreposta ao conteúdo para preservar o tamanho dos controles. WindowOutline.Wrap está disponível na biblioteca do instalador para outras janelas de software.
