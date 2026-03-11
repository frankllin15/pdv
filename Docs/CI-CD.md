# CI/CD - PDV

Documentação da infraestrutura de integração e entrega contínua do projeto PDV.

---

## Visão Geral

```
                           Push / PR para main
                                  │
                                  ▼
                        ┌─────────────────┐
                        │   CI Pipeline   │
                        │  (ci.yml)       │
                        │  Build + Test   │
                        └─────────────────┘

                            Push tag v*
                                  │
                      ┌───────────┴───────────┐
                      ▼                       ▼
            ┌──────────────────┐    ┌──────────────────┐
            │  Release Desktop │    │   Release API    │
            │ (release-desktop │    │ (release-api.yml)│
            │      .yml)       │    │                  │
            │                  │    │                  │
            │ Velopack + GitHub│    │ Docker + GHCR    │
            │    Releases      │    │                  │
            └──────────────────┘    └──────────────────┘
```

O projeto possui **3 workflows** no GitHub Actions:

| Workflow | Arquivo | Trigger | Runner |
|----------|---------|---------|--------|
| CI | `ci.yml` | Push/PR em `main` | `windows-latest` |
| Release Desktop | `release-desktop.yml` | Tag `v*` | `windows-latest` |
| Release API | `release-api.yml` | Tag `v*` | `ubuntu-latest` |

---

## 1. Versionamento Centralizado

A versão de todos os projetos da solução é definida em um único lugar:

```
PDV/
├── Directory.Build.props   ← fonte única de verdade
├── PDV.sln
└── ...
```

O `Directory.Build.props` define `Version`, `AssemblyVersion`, `FileVersion` e `InformationalVersion`. Todos os 9 projetos herdam essas propriedades automaticamente.

Durante o CD, a versão é sobrescrita pela tag do git via MSBuild:

```
-p:Version=1.2.3 -p:InformationalVersion=1.2.3+abc1234
```

Isso garante que a versão no binário sempre corresponda à tag que gerou o release.

---

## 2. Pipeline de CI (`ci.yml`)

Roda em **todo push e PR** para a branch `main`.

### O que faz

1. Checkout do código
2. Setup do .NET 9
3. Cache de pacotes NuGet (baseado no hash dos `.csproj`)
4. `dotnet restore`
5. `dotnet build -c Release -warnaserror` (warnings tratados como erros)
6. `dotnet test` com cobertura de código (XPlat Code Coverage)
7. Upload dos resultados de teste como artefato (retenção: 30 dias)

### Concorrência

O workflow usa `cancel-in-progress: true` — se um push novo chega enquanto o anterior ainda está rodando, o anterior é cancelado. Isso economiza minutos de CI em pushes rápidos.

### Artefatos gerados

| Artefato | Conteúdo |
|----------|----------|
| `test-results` | Arquivo `.trx` com resultados dos testes + relatório de cobertura |

---

## 3. Release Desktop (`release-desktop.yml`)

Roda quando uma **tag `v*`** é criada (ex: `v1.0.0`, `v2.1.0-beta.1`).

### O que faz

1. Checkout com histórico completo (`fetch-depth: 0`) para gerar release notes
2. Setup do .NET 9 + cache NuGet
3. Extrai a versão da tag (remove o prefixo `v`)
4. Build da solução inteira com versão injetada via MSBuild
5. **Executa testes** antes de empacotar (garantia de confiabilidade)
6. `dotnet publish` self-contained, win-x64, single file
7. `vpk pack` (Velopack CLI) gera Setup.exe, .nupkg, deltas e RELEASES
8. Gera release notes automáticas a partir dos commits entre tags
9. Cria GitHub Release com os artefatos

### Prerelease automático

Tags com `-` no nome são marcadas como prerelease:

- `v1.0.0` → Release estável
- `v1.0.0-beta.1` → Prerelease

### Artefatos no GitHub Release

| Arquivo | Descrição |
|---------|-----------|
| `Setup.exe` | Instalador para primeira instalação |
| `*-full.nupkg` | Pacote completo da versão |
| `*-delta.nupkg` | Pacote incremental (diferença entre versões) |
| `RELEASES` | Índice que o Velopack lê para verificar atualizações |

---

## 4. Release API (`release-api.yml`)

Roda no **mesmo trigger** do Desktop (tag `v*`), mas em `ubuntu-latest` (mais rápido e barato).

### O que faz

1. Checkout + Setup .NET 9
2. Extrai versão da tag
3. Build + testes
4. `dotnet publish` da API
5. Upload do artefato de publicação
6. Build da imagem Docker (multi-stage)
7. Push para GitHub Container Registry (GHCR)

### Imagem Docker

```
ghcr.io/<owner>/pdv-api:1.2.3     ← tag específica
ghcr.io/<owner>/pdv-api:latest     ← sempre a última versão
```

O `Dockerfile` em `Presentation/PDV.API/Dockerfile` usa multi-stage build:

- **Stage 1 (sdk:9.0):** Restore, build e publish
- **Stage 2 (aspnet:9.0):** Runtime mínimo, porta 8080

---

## 5. Atualização Automática (Velopack)

O Desktop integra o Velopack para atualizações automáticas.

### Fluxo no código

```
Program.Main()
    │
    ├── VelopackApp.Build().Run()    ← hooks de instalação/desinstalação
    │
    ├── BuildAvaloniaApp().Start()   ← inicia o Avalonia
    │
    └── CheckForUpdatesAsync()       ← chamado em background após o app abrir
            │
            ├── IsInstalled? → false → skip (modo dev)
            ├── CheckForUpdatesAsync() → null → sem atualização
            └── DownloadUpdatesAsync() → baixa, aplica no próximo restart
```

### Decisões de design

- **Sem auto-restart:** O PDV não reinicia sozinho durante operação do caixa. A atualização é aplicada no próximo início manual.
- **Guard `IsInstalled`:** Em modo desenvolvimento (F5 no Visual Studio), o Velopack é ignorado silenciosamente.
- **Falha silenciosa:** Se não houver internet, o PDV continua funcionando normalmente. Erros de atualização são registrados como warning no Serilog.

---

## 6. Dependabot

O arquivo `.github/dependabot.yml` configura atualizações automáticas:

- **NuGet:** Verifica semanalmente, até 5 PRs abertas
- **GitHub Actions:** Verifica semanalmente, até 5 PRs abertas

O Dependabot cria PRs automaticamente quando novas versões de pacotes ou actions são lançadas. Essas PRs passam pelo CI antes de serem mergeadas.

---

## Como Fazer um Release

### Passo a passo

```bash
# 1. Certifique-se de estar na main com tudo commitado
git checkout main
git pull origin main

# 2. Crie a tag com a versão desejada
git tag v1.0.0

# 3. Faça push da tag
git push origin v1.0.0

# 4. Acompanhe os workflows na aba Actions do GitHub
```

Os dois workflows de release (Desktop e API) rodam em paralelo automaticamente.

### Convenção de versionamento (SemVer)

```
v<MAJOR>.<MINOR>.<PATCH>[-<prerelease>]

Exemplos:
  v1.0.0          → Primeira versão estável
  v1.1.0          → Nova funcionalidade (retrocompatível)
  v1.1.1          → Correção de bug
  v2.0.0          → Breaking change
  v2.0.0-beta.1   → Versão de teste (prerelease)
```

### Checklist pré-release

- [ ] Todos os testes passando localmente (`dotnet test`)
- [ ] Versão no `Directory.Build.props` atualizada (opcional — o CI sobrescreve via tag)
- [ ] Changelog ou notas relevantes commitadas
- [ ] Branch `main` estável e atualizada

---

## Estrutura de Arquivos

```
PDV/
├── Directory.Build.props                    ← versionamento centralizado
├── .dockerignore                            ← exclusões para build Docker
├── .github/
│   ├── dependabot.yml                       ← atualização automática de dependências
│   └── workflows/
│       ├── ci.yml                           ← pipeline de CI
│       ├── release-desktop.yml              ← CD do Desktop (Velopack + GitHub Releases)
│       └── release-api.yml                  ← CD da API (Docker + GHCR)
├── Presentation/
│   ├── PDV.Desktop/
│   │   ├── Program.cs                       ← integração Velopack
│   │   └── PDV.Desktop.csproj               ← PackageReference Velopack
│   └── PDV.API/
│       └── Dockerfile                       ← imagem Docker da API
└── Core/
    └── ci-cd.md                             ← especificação original do Velopack
```

---

## Configuração Necessária

### Repositório público

Nenhuma configuração extra. O `GITHUB_TOKEN` padrão tem permissões suficientes para:
- Criar GitHub Releases (Desktop)
- Push para GHCR (API)

### Repositório privado

Para o Velopack conseguir consultar os releases no Desktop:

1. Crie um **Personal Access Token (PAT)** no GitHub com permissão `Contents: Read-only`
2. Atualize a URL no `Program.cs` passando o token para o `UpdateManager`

> **Segurança:** Use um PAT com privilégios mínimos. Se extraído por engenharia reversa, não compromete o código-fonte.

### URL do UpdateManager

Atualize a URL em `Presentation/PDV.Desktop/Program.cs` na linha do `new UpdateManager(...)`:

```csharp
var mgr = new UpdateManager("https://github.com/SEU_USUARIO/PDV/releases/latest");
```
