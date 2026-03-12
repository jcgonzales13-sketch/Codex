using ERP.BuildingBlocks;
using ERP.Api.Application.Contracts;
using ERP.Api.Application.Security;
using Microsoft.Net.Http.Headers;

namespace ERP.Api.Application;

public static class ErpEndpoints
{
    private const string SessionHeader = "X-Session-Token";

    public static IEndpointRouteBuilder MapErpEndpoints(this IEndpointRouteBuilder app)
    {
        var empresas = app.MapGroup("/empresas").WithTags("Empresas");
        empresas.MapGet(string.Empty, (string? status, string? termo, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarEmpresas(new ConsultarEmpresasRequest(status, termo, page ?? 1, pageSize ?? 20)))))
            .WithSummary("Consulta empresas.")
            .WithDescription("Lista empresas com filtros opcionais por status, termo de busca e paginacao. Use este endpoint para montar seletores administrativos e validar o contexto empresarial disponivel.")
            .Produces(StatusCodes.Status200OK);
        empresas.MapPost(string.Empty, (HttpContext httpContext, CreateEmpresaRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.EmpresasManage);
            var response = service.CadastrarEmpresa(request);
            return Results.Created($"/empresas/{response.Id}", ApiResponses.Ok(response));
        })
        .WithSummary("Cadastra uma empresa.")
        .WithDescription("Cria uma nova empresa operacional no ERP e inicializa os perfis padrao de acesso usados pelo modulo de Identity.")
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
        empresas.MapPost("/{empresaId:guid}/atualizar", (HttpContext httpContext, Guid empresaId, AtualizarEmpresaRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.EmpresasManage);
            return Results.Ok(ApiResponses.Ok(service.AtualizarEmpresa(empresaId, request)));
        })
        .WithSummary("Atualiza dados cadastrais da empresa.")
        .WithDescription("Atualiza nome fantasia e razao social da empresa informada, preservando historico e contexto operacional ja existente.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
        empresas.MapPost("/{empresaId:guid}/ativar", (HttpContext httpContext, Guid empresaId, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.EmpresasManage);
            return Results.Ok(ApiResponses.Ok(service.AtivarEmpresa(empresaId)));
        })
        .WithSummary("Ativa uma empresa.")
        .WithDescription("Reativa a empresa para operacao normal, voltando a permitir novos cadastros e fluxos de negocio vinculados a ela.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
        empresas.MapPost("/{empresaId:guid}/inativar", (HttpContext httpContext, Guid empresaId, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.EmpresasManage);
            return Results.Ok(ApiResponses.Ok(service.InativarEmpresa(empresaId)));
        })
        .WithSummary("Inativa uma empresa.")
        .WithDescription("Impede novas operacoes vinculadas a empresa, mantendo seus dados historicos para consulta e auditoria.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
        empresas.MapPost("/{empresaId:guid}/bloquear", (HttpContext httpContext, Guid empresaId, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.EmpresasManage);
            return Results.Ok(ApiResponses.Ok(service.BloquearEmpresa(empresaId)));
        })
        .WithSummary("Bloqueia uma empresa.")
        .WithDescription("Bloqueia a empresa para interromper operacoes de negocio de forma administrativa, sem excluir ou perder o historico existente.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        var catalogo = app.MapGroup("/catalogo").WithTags("Catalogo");
        catalogo.MapGet("/produtos", (Guid? empresaId, bool? ativo, string? termo, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarProdutos(new ConsultarProdutosRequest(empresaId, ativo, termo, page ?? 1, pageSize ?? 20)))))
            .WithSummary("Consulta produtos.")
            .WithDescription("Lista produtos por empresa, status ativo, termo e paginacao. Adequado para pesquisa operacional e montagem de catalogo no frontend.")
            .Produces(StatusCodes.Status200OK);
        catalogo.MapPost("/produtos", (HttpContext httpContext, CreateProdutoRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.CatalogoManage, request.EmpresaId);
            var response = service.CadastrarProduto(request);
            return Results.Created($"/catalogo/produtos/{response.Id}", ApiResponses.Ok(response));
        })
        .WithSummary("Cadastra um produto.")
        .WithDescription("Cria um produto com dados comerciais e fiscais, incluindo identificacao basica para venda, estoque e emissao fiscal.")
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
        catalogo.MapPost("/produtos/{produtoId:guid}/inativar", (HttpContext httpContext, Guid produtoId, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.CatalogoManage, service.ObterEmpresaIdDoProduto(produtoId));
            return Results.Ok(ApiResponses.Ok(service.InativarProduto(produtoId)));
        })
        .WithSummary("Inativa um produto.")
        .WithDescription("Retira o produto de operacao mantendo historico, impedindo novas movimentacoes comerciais com ele.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
        catalogo.MapPost("/produtos/{produtoId:guid}/variacoes", (HttpContext httpContext, Guid produtoId, AddVariacaoRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.CatalogoManage, service.ObterEmpresaIdDoProduto(produtoId));
            return Results.Ok(ApiResponses.Ok(service.AdicionarVariacao(produtoId, request)));
        })
        .WithSummary("Adiciona variacao a um produto.")
        .WithDescription("Acrescenta SKU ou variacao comercial ao produto informado, permitindo desdobrar apresentacoes diferentes do mesmo item base.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
        catalogo.MapPost("/produtos/{produtoId:guid}/dados-fiscais", (HttpContext httpContext, Guid produtoId, UpdateFiscalProdutoRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.CatalogoManage, service.ObterEmpresaIdDoProduto(produtoId));
            return Results.Ok(ApiResponses.Ok(service.AtualizarFiscalProduto(produtoId, request)));
        })
        .WithSummary("Atualiza dados fiscais do produto.")
        .WithDescription("Atualiza NCM e origem fiscal do produto para manter conformidade no fluxo de emissao de notas.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        var clientes = app.MapGroup("/clientes").WithTags("Clientes");
        clientes.MapGet(string.Empty, (Guid? empresaId, string? status, string? termo, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarClientes(new ConsultarClientesRequest(empresaId, status, termo, page ?? 1, pageSize ?? 20)))))
            .WithSummary("Consulta clientes.")
            .WithDescription("Lista clientes com filtros por empresa, status, termo e paginacao. Use para pesquisa comercial e suporte ao fluxo de vendas.")
            .Produces(StatusCodes.Status200OK);
        clientes.MapPost(string.Empty, (HttpContext httpContext, CreateClienteRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.ClientesManage, request.EmpresaId);
            var response = service.CadastrarCliente(request);
            return Results.Created($"/clientes/{response.Id}", ApiResponses.Ok(response));
        })
        .WithSummary("Cadastra um cliente.")
        .WithDescription("Cria um cliente operacional para vendas e fiscal, habilitando o uso do cadastro em pedidos e notas.")
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
        clientes.MapPost("/{clienteId:guid}/atualizar", (HttpContext httpContext, Guid clienteId, AtualizarClienteRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.ClientesManage, service.ObterEmpresaIdDoCliente(clienteId));
            return Results.Ok(ApiResponses.Ok(service.AtualizarCliente(clienteId, request)));
        })
        .WithSummary("Atualiza um cliente.")
        .WithDescription("Atualiza nome e email do cliente, preservando seus vinculos operacionais ja existentes.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
        clientes.MapPost("/{clienteId:guid}/ativar", (HttpContext httpContext, Guid clienteId, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.ClientesManage, service.ObterEmpresaIdDoCliente(clienteId));
            return Results.Ok(ApiResponses.Ok(service.AtivarCliente(clienteId)));
        })
        .WithSummary("Ativa um cliente.")
        .WithDescription("Reativa o cliente para operacao e volta a permitir novos pedidos e documentos vinculados a ele.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
        clientes.MapPost("/{clienteId:guid}/inativar", (HttpContext httpContext, Guid clienteId, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.ClientesManage, service.ObterEmpresaIdDoCliente(clienteId));
            return Results.Ok(ApiResponses.Ok(service.InativarCliente(clienteId)));
        })
        .WithSummary("Inativa um cliente.")
        .WithDescription("Inativa o cliente para novas operacoes, mantendo o historico de relacionamento ja registrado.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
        clientes.MapPost("/{clienteId:guid}/bloquear", (HttpContext httpContext, Guid clienteId, BloquearClienteRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.ClientesManage, service.ObterEmpresaIdDoCliente(clienteId));
            return Results.Ok(ApiResponses.Ok(service.BloquearCliente(clienteId, request)));
        })
        .WithSummary("Bloqueia um cliente.")
        .WithDescription("Bloqueia o cliente com registro de motivo para impedir continuidade comercial por decisao administrativa.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        var fornecedores = app.MapGroup("/fornecedores").WithTags("Fornecedores");
        fornecedores.MapGet(string.Empty, (Guid? empresaId, string? status, string? termo, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarFornecedores(new ConsultarFornecedoresRequest(empresaId, status, termo, page ?? 1, pageSize ?? 20)))))
            .WithSummary("Consulta fornecedores.")
            .WithDescription("Lista fornecedores com filtros por empresa, status, termo e paginacao. Apoia compras, conciliacao e auditoria de origem dos documentos de entrada.")
            .Produces(StatusCodes.Status200OK);
        fornecedores.MapPost(string.Empty, (HttpContext httpContext, CreateFornecedorRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.FornecedoresManage, request.EmpresaId);
            var response = service.CadastrarFornecedor(request);
            return Results.Created($"/fornecedores/{response.Id}", ApiResponses.Ok(response));
        })
        .WithSummary("Cadastra um fornecedor.")
        .WithDescription("Cria um fornecedor para operacoes de compras, vinculando-o a uma empresa e habilitando seu uso nas importacoes de nota.")
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
        fornecedores.MapPost("/{fornecedorId:guid}/atualizar", (HttpContext httpContext, Guid fornecedorId, AtualizarFornecedorRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.FornecedoresManage, service.ObterEmpresaIdDoFornecedor(fornecedorId));
            return Results.Ok(ApiResponses.Ok(service.AtualizarFornecedor(fornecedorId, request)));
        })
        .WithSummary("Atualiza um fornecedor.")
        .WithDescription("Atualiza nome e email do fornecedor sem invalidar o historico de compras ja registradas.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
        fornecedores.MapPost("/{fornecedorId:guid}/ativar", (HttpContext httpContext, Guid fornecedorId, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.FornecedoresManage, service.ObterEmpresaIdDoFornecedor(fornecedorId));
            return Results.Ok(ApiResponses.Ok(service.AtivarFornecedor(fornecedorId)));
        })
        .WithSummary("Ativa um fornecedor.")
        .WithDescription("Reativa o fornecedor para compras e novas importacoes de documentos de entrada.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
        fornecedores.MapPost("/{fornecedorId:guid}/inativar", (HttpContext httpContext, Guid fornecedorId, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.FornecedoresManage, service.ObterEmpresaIdDoFornecedor(fornecedorId));
            return Results.Ok(ApiResponses.Ok(service.InativarFornecedor(fornecedorId)));
        })
        .WithSummary("Inativa um fornecedor.")
        .WithDescription("Inativa o fornecedor para novas operacoes, sem remover o historico de compras ja importadas.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
        fornecedores.MapPost("/{fornecedorId:guid}/bloquear", (HttpContext httpContext, Guid fornecedorId, BloquearFornecedorRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.FornecedoresManage, service.ObterEmpresaIdDoFornecedor(fornecedorId));
            return Results.Ok(ApiResponses.Ok(service.BloquearFornecedor(fornecedorId, request)));
        })
        .WithSummary("Bloqueia um fornecedor.")
        .WithDescription("Bloqueia o fornecedor com registro de motivo para interromper seu uso operacional por decisao administrativa.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        var depositos = app.MapGroup("/depositos").WithTags("Depositos");
        depositos.MapGet(string.Empty, (Guid? empresaId, string? status, string? termo, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarDepositos(new ConsultarDepositosRequest(empresaId, status, termo, page ?? 1, pageSize ?? 20)))))
            .WithSummary("Consulta depositos.")
            .WithDescription("Lista depositos por empresa, status, termo e paginacao. Use para selecao operacional de armazenagem e roteamento de estoque.")
            .Produces(StatusCodes.Status200OK);
        depositos.MapPost(string.Empty, (HttpContext httpContext, CreateDepositoRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.DepositosManage, request.EmpresaId);
            var response = service.CadastrarDeposito(request);
            return Results.Created($"/depositos/{response.Id}", ApiResponses.Ok(response));
        })
        .WithSummary("Cadastra um deposito.")
        .WithDescription("Cria um deposito para operacoes de estoque, transferencias e reservas logicas de produtos.")
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
        depositos.MapPost("/{depositoId:guid}/atualizar", (HttpContext httpContext, Guid depositoId, AtualizarDepositoRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.DepositosManage, service.ObterEmpresaIdDoDeposito(depositoId));
            return Results.Ok(ApiResponses.Ok(service.AtualizarDeposito(depositoId, request)));
        })
        .WithSummary("Atualiza um deposito.")
        .WithDescription("Atualiza o nome do deposito sem alterar seu identificador nem os saldos ja mantidos nele.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
        depositos.MapPost("/{depositoId:guid}/ativar", (HttpContext httpContext, Guid depositoId, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.DepositosManage, service.ObterEmpresaIdDoDeposito(depositoId));
            return Results.Ok(ApiResponses.Ok(service.AtivarDeposito(depositoId)));
        })
        .WithSummary("Ativa um deposito.")
        .WithDescription("Reativa o deposito para operacoes de estoque, compras e vendas vinculadas.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
        depositos.MapPost("/{depositoId:guid}/inativar", (HttpContext httpContext, Guid depositoId, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.DepositosManage, service.ObterEmpresaIdDoDeposito(depositoId));
            return Results.Ok(ApiResponses.Ok(service.InativarDeposito(depositoId)));
        })
        .WithSummary("Inativa um deposito.")
        .WithDescription("Inativa o deposito para novas movimentacoes, mantendo os registros historicos associados.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        var identity = app.MapGroup("/identity").WithTags("Identity");
        identity.MapGet("/permissoes", (ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ListarPermissoes())))
            .WithSummary("Lista permissoes reconhecidas pela API.")
            .Produces(StatusCodes.Status200OK);
        identity.MapGet("/perfis/padroes", (ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ListarPerfisAcessoPadrao())))
            .WithSummary("Lista os perfis padrao disponibilizados para novas empresas.")
            .WithDescription("Retorna o catalogo de perfis padrao que a aplicacao cria automaticamente quando uma empresa e cadastrada.")
            .Produces(StatusCodes.Status200OK);
        identity.MapGet("/perfis", (Guid? empresaId, string? termo, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarPerfisAcesso(new ConsultarPerfisAcessoRequest(empresaId, termo, page ?? 1, pageSize ?? 20)))))
            .WithSummary("Consulta perfis de acesso por empresa.")
            .WithDescription("Lista perfis de acesso por empresa com suporte a filtro textual e paginacao para administracao do modulo Identity.")
            .Produces(StatusCodes.Status200OK);
        identity.MapGet("/usuarios", (Guid? empresaId, string? status, string? termo, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarUsuarios(new ConsultarUsuariosRequest(empresaId, status, termo, page ?? 1, pageSize ?? 20)))))
            .WithSummary("Consulta usuarios por empresa.")
            .WithDescription("Lista usuarios por empresa com filtros por status e termo de busca, retornando tambem permissoes e perfis associados.")
            .Produces(StatusCodes.Status200OK);
        identity.MapPost("/perfis", (HttpContext httpContext, CreatePerfilAcessoRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.IdentityManage, request.EmpresaId);
            var response = service.CadastrarPerfilAcesso(request);
            return Results.Created($"/identity/perfis/{response.Id}", ApiResponses.Ok(response));
        })
        .WithSummary("Cadastra um perfil de acesso.")
        .WithDescription("Cria um perfil de acesso reutilizavel com um conjunto de permissoes para ser associado a usuarios da mesma empresa.")
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status400BadRequest);
        identity.MapPost("/perfis/{perfilAcessoId:guid}/atualizar", (HttpContext httpContext, Guid perfilAcessoId, AtualizarPerfilAcessoRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.IdentityManage, service.ObterEmpresaIdDoPerfilAcesso(perfilAcessoId));
            return Results.Ok(ApiResponses.Ok(service.AtualizarPerfilAcesso(perfilAcessoId, request)));
        })
        .WithSummary("Atualiza um perfil de acesso existente.")
        .WithDescription("Atualiza nome e permissoes de um perfil de acesso existente, refletindo o efeito nos usuarios vinculados a ele.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);
        identity.MapPost("/usuarios", (HttpContext httpContext, CreateUsuarioRequest request, ErpApplicationService service) =>
        {
            if (!service.PermiteBootstrapIdentity(request.EmpresaId))
            {
                RequirePermission(httpContext, service, IdentityPermissions.IdentityManage, request.EmpresaId);
            }
            var response = service.CadastrarUsuario(request);
            return Results.Created($"/identity/usuarios/{response.Id}", ApiResponses.Ok(response));
        })
        .WithSummary("Cadastra um usuario. O primeiro usuario da empresa recebe bootstrap administrativo.")
        .WithDescription("Quando for o primeiro usuario da empresa, a resposta retorna bootstrapAdministrador=true e o usuario recebe o perfil Administrador.")
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status400BadRequest);
        identity.MapPost("/usuarios/{usuarioId:guid}/senha", (HttpContext httpContext, Guid usuarioId, DefinirSenhaUsuarioRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.IdentityManage, service.ObterEmpresaIdDoUsuario(usuarioId));
            return Results.Ok(ApiResponses.Ok(service.DefinirSenhaUsuario(usuarioId, request)));
        })
        .WithSummary("Define ou altera a senha do usuario.")
        .WithDescription("Configura ou altera a senha do usuario. Este endpoint pode ser usado no bootstrap inicial ou em manutencao administrativa de credenciais.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
        identity.MapPost("/usuarios/{usuarioId:guid}/ativar", (HttpContext httpContext, Guid usuarioId, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.IdentityManage, service.ObterEmpresaIdDoUsuario(usuarioId));
            return Results.Ok(ApiResponses.Ok(service.AtivarUsuario(usuarioId)));
        })
        .WithSummary("Ativa um usuario.")
        .WithDescription("Reativa um usuario para autenticacao e para novas operacoes protegidas na API.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
        identity.MapPost("/usuarios/{usuarioId:guid}/bloquear", (HttpContext httpContext, Guid usuarioId, BloquearUsuarioRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.IdentityManage, service.ObterEmpresaIdDoUsuario(usuarioId));
            return Results.Ok(ApiResponses.Ok(service.BloquearUsuario(usuarioId, request)));
        })
        .WithSummary("Bloqueia um usuario.")
        .WithDescription("Bloqueia o usuario impedindo novos acessos, sem remover seu historico de sessoes e permissoes registradas.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
        identity.MapPost("/usuarios/{usuarioId:guid}/permissoes", (HttpContext httpContext, Guid usuarioId, ConcederPermissaoRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.IdentityManage, service.ObterEmpresaIdDoUsuario(usuarioId));
            return Results.Ok(ApiResponses.Ok(service.ConcederPermissao(usuarioId, request)));
        })
        .WithSummary("Concede permissao direta a um usuario.")
        .WithDescription("Adiciona uma permissao individual ao usuario, complementar aos perfis de acesso ja vinculados.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
        identity.MapPost("/usuarios/{usuarioId:guid}/perfis", (HttpContext httpContext, Guid usuarioId, VincularPerfilAcessoRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.IdentityManage, service.ObterEmpresaIdDoUsuario(usuarioId));
            return Results.Ok(ApiResponses.Ok(service.VincularPerfilAcesso(usuarioId, request)));
        })
        .WithSummary("Vincula perfil de acesso ao usuario.")
        .WithDescription("Associa um perfil existente ao usuario para herdar permissoes padronizadas da mesma empresa.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
        identity.MapPost("/auth/login", (LoginRequest request, ErpApplicationService service) => Results.Ok(ApiResponses.Ok(service.Login(request))))
            .WithSummary("Realiza login baseado em sessao interna.")
            .WithDescription("Autentica um usuario por empresa, email e senha, retornando o token de sessao interna usado pela API.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);
        identity.MapPost("/oauth/token", (TokenRequest request, ErpApplicationService service, JwtTokenService jwtTokenService) =>
        {
            var sessao = service.Login(new LoginRequest(request.EmpresaId, request.Email, request.Senha));
            var refreshToken = jwtTokenService.GenerateRefreshToken();
            service.RegistrarRefreshToken(sessao.Token, refreshToken, jwtTokenService.GetRefreshTokenExpiration());
            return Results.Ok(ApiResponses.Ok(new TokenResponse(
                jwtTokenService.GenerateAccessToken(sessao),
                refreshToken,
                "Bearer",
                sessao.ExpiresAt,
                sessao)));
        })
        .WithSummary("Emite um access token JWT e um refresh token.")
        .WithDescription("Usa as credenciais do usuario para abrir uma sessao e retornar tokens bearer prontos para uso em Authorization.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);
        identity.MapPost("/oauth/refresh", (RefreshTokenRequest request, ErpApplicationService service, JwtTokenService jwtTokenService) =>
        {
            var sessao = service.RenovarSessaoComRefreshToken(request);
            var refreshToken = jwtTokenService.GenerateRefreshToken();
            service.RegistrarRefreshToken(sessao.Token, refreshToken, jwtTokenService.GetRefreshTokenExpiration());
            return Results.Ok(ApiResponses.Ok(new TokenResponse(
                jwtTokenService.GenerateAccessToken(sessao),
                refreshToken,
                "Bearer",
                sessao.ExpiresAt,
                sessao)));
        })
        .WithSummary("Renova o access token JWT a partir de um refresh token valido.")
        .WithDescription("Troca um refresh token valido por um novo access token JWT e um novo refresh token, mantendo a sessao vinculada ao usuario.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);
        identity.MapPost("/auth/logout", (HttpContext httpContext, LogoutRequest request, ErpApplicationService service) =>
        {
            var sessionToken = !string.IsNullOrWhiteSpace(request.Token) ? request.Token! : ResolveSessionToken(httpContext);
            service.Logout(new LogoutRequest(sessionToken));
            return Results.Ok(ApiResponses.Ok(new { success = true }));
        })
        .WithSummary("Encerra a sessao atual.")
        .WithDescription("Encerra a sessao autenticada. Pode receber o token explicitamente no corpo ou resolver a sessao atual pelo header enviado.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound);
        identity.MapPost("/auth/sessao", (ConsultarSessaoRequest request, ErpApplicationService service) => Results.Ok(ApiResponses.Ok(service.ConsultarSessao(request))))
            .WithSummary("Consulta a sessao autenticada.")
            .WithDescription("Retorna os dados atuais da sessao interna a partir do token informado, incluindo permissoes efetivas e contexto da empresa.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        var estoque = app.MapGroup("/estoque").WithTags("Estoque");
        estoque.MapGet("/saldos", (Guid? produtoId, Guid? depositoId, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarSaldos(new ConsultarSaldosEstoqueRequest(produtoId, depositoId, page ?? 1, pageSize ?? 20)))))
            .WithSummary("Consulta saldos de estoque.")
            .WithDescription("Lista saldos por produto, deposito e paginacao. Use este endpoint para visao atual de disponibilidade operacional.")
            .Produces(StatusCodes.Status200OK);
        estoque.MapGet("/movimentos", (Guid? produtoId, Guid? depositoId, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarMovimentosEstoque(new ConsultarMovimentosEstoqueRequest(produtoId, depositoId, page ?? 1, pageSize ?? 20)))))
            .WithSummary("Consulta movimentos de estoque.")
            .WithDescription("Lista o historico de movimentos por produto, deposito e paginacao. Adequado para auditoria, suporte e rastreabilidade operacional.")
            .Produces(StatusCodes.Status200OK);
        estoque.MapPost("/saldos", (HttpContext httpContext, CriarSaldoEstoqueRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.EstoqueManage, service.ObterEmpresaIdDoDeposito(request.DepositoId));
            return Results.Created($"/estoque/saldos/{request.ProdutoId}/{request.DepositoId}", ApiResponses.Ok(service.CriarSaldo(request)));
        })
        .WithSummary("Cria saldo inicial de estoque.")
        .WithDescription("Cria o saldo operacional inicial de um produto em um deposito. Use para bootstrap de estoque ou ativacao inicial do item.")
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
        estoque.MapPost("/saldos/ajustes", (HttpContext httpContext, AjustarSaldoRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.EstoqueManage, service.ObterEmpresaIdDoDeposito(request.DepositoId));
            return Results.Ok(ApiResponses.Ok(service.AjustarSaldo(request)));
        })
        .WithSummary("Ajusta saldo de estoque.")
        .WithDescription("Aplica ajuste manual positivo ou negativo ao saldo, registrando a movimentacao correspondente para auditoria.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
        estoque.MapPost("/saldos/reservas", (HttpContext httpContext, ReservarSaldoRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.EstoqueManage, service.ObterEmpresaIdDoDeposito(request.DepositoId));
            return Results.Ok(ApiResponses.Ok(service.ReservarSaldo(request)));
        })
        .WithSummary("Reserva saldo de estoque.")
        .WithDescription("Reserva quantidade disponivel para operacao futura, reduzindo a disponibilidade livre sem baixar o estoque definitivo.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
        estoque.MapPost("/saldos/baixas", (HttpContext httpContext, ConfirmarBaixaRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.EstoqueManage, service.ObterEmpresaIdDoDeposito(request.DepositoId));
            return Results.Ok(ApiResponses.Ok(service.ConfirmarBaixaFaturamento(request)));
        })
        .WithSummary("Confirma baixa de estoque.")
        .WithDescription("Consolida baixa operacional de estoque apos faturamento ou outra operacao definitiva de saida.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
        estoque.MapPost("/transferencias", (HttpContext httpContext, TransferirEstoqueRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.EstoqueManage, service.ObterEmpresaIdDoDeposito(request.DepositoOrigemId));
            return Results.Ok(ApiResponses.Ok(service.Transferir(request)));
        })
        .WithSummary("Transfere estoque entre depositos.")
        .WithDescription("Move saldo de um deposito de origem para um deposito de destino, registrando saida e entrada no historico de movimentos.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        var vendas = app.MapGroup("/vendas").WithTags("Vendas");
        vendas.MapGet("/pedidos", (string? status, Guid? clienteId, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarPedidos(new ConsultarPedidosVendaRequest(status, clienteId, page ?? 1, pageSize ?? 20)))))
            .WithSummary("Consulta pedidos de venda.")
            .WithDescription("Lista pedidos por status, cliente e paginacao. Use para acompanhamento comercial e operacional do ciclo de vendas.")
            .Produces(StatusCodes.Status200OK);
        vendas.MapPost("/pedidos", (HttpContext httpContext, CreatePedidoVendaRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.VendasManage, service.ObterEmpresaIdDoCliente(request.ClienteId));
            var response = service.CriarPedido(request);
            return Results.Created($"/vendas/pedidos/{response.Id}", ApiResponses.Ok(response));
        })
        .WithSummary("Cria pedido de venda.")
        .WithDescription("Abre um novo pedido de venda para o cliente informado, validando o contexto empresarial do cadastro.")
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
        vendas.MapPost("/pedidos/{pedidoId:guid}/itens", (HttpContext httpContext, Guid pedidoId, AddItemPedidoRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.VendasManage, service.ObterEmpresaIdDoPedido(pedidoId));
            return Results.Ok(ApiResponses.Ok(service.AdicionarItemPedido(pedidoId, request)));
        })
        .WithSummary("Adiciona item ao pedido.")
        .WithDescription("Inclui produto, quantidade e preco no pedido, respeitando os bloqueios de consistencia do fluxo comercial.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
        vendas.MapPost("/pedidos/{pedidoId:guid}/aprovar", (HttpContext httpContext, Guid pedidoId, AprovarPedidoRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.VendasManage, service.ObterEmpresaIdDoPedido(pedidoId));
            return Results.Ok(ApiResponses.Ok(service.AprovarPedido(pedidoId, request)));
        })
        .WithSummary("Aprova pedido de venda.")
        .WithDescription("Move o pedido para etapa de aprovacao comercial, validando cliente e consistencia basica antes da reserva.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
        vendas.MapPost("/pedidos/{pedidoId:guid}/reservar", (HttpContext httpContext, Guid pedidoId, ReservarPedidoRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.VendasManage, service.ObterEmpresaIdDoPedido(pedidoId));
            return Results.Ok(ApiResponses.Ok(service.ReservarPedido(pedidoId, request)));
        })
        .WithSummary("Reserva estoque para o pedido.")
        .WithDescription("Reserva o estoque do deposito para os itens do pedido, preparando o fluxo para faturamento e emissao fiscal.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
        vendas.MapPost("/pedidos/{pedidoId:guid}/cancelar", (HttpContext httpContext, Guid pedidoId, CancelarPedidoRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.VendasManage, service.ObterEmpresaIdDoPedido(pedidoId));
            return Results.Ok(ApiResponses.Ok(service.CancelarPedido(pedidoId, request)));
        })
        .WithSummary("Cancela pedido de venda.")
        .WithDescription("Cancela o pedido e pode liberar reserva de estoque, conforme o payload informado e o estado atual do fluxo.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        var compras = app.MapGroup("/compras").WithTags("Compras");
        compras.MapGet("/importacoes-nota-entrada", (Guid? empresaId, Guid? fornecedorId, Guid? depositoId, bool? importadaComSucesso, string? chaveAcesso, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarImportacoesNotaEntrada(new ConsultarImportacoesNotaEntradaRequest(empresaId, fornecedorId, depositoId, importadaComSucesso, chaveAcesso, page ?? 1, pageSize ?? 20)))))
            .WithSummary("Consulta importacoes de nota de entrada.")
            .WithDescription("Lista o historico de importacoes de compras com filtros operacionais por empresa, fornecedor, deposito, sucesso e chave de acesso.")
            .Produces(StatusCodes.Status200OK);
        compras.MapPost("/importacoes-nota-entrada", (HttpContext httpContext, ImportarNotaEntradaRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.ComprasManage, request.EmpresaId);
            return Results.Ok(ApiResponses.Ok(service.ImportarNotaEntrada(request)));
        })
        .WithSummary("Importa nota de entrada.")
        .WithDescription("Importa itens externos de compra, concilia produtos e gera entrada de estoque quando aplicavel, com protecao de idempotencia por chave de acesso.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        var fiscal = app.MapGroup("/fiscal").WithTags("Fiscal");
        fiscal.MapGet("/notas", (string? status, Guid? clienteId, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarNotasFiscais(new ConsultarNotasFiscaisRequest(status, clienteId, page ?? 1, pageSize ?? 20)))))
            .WithSummary("Consulta notas fiscais.")
            .WithDescription("Lista notas fiscais por status, cliente e paginacao. Use para acompanhamento do ciclo fiscal e conciliacao de faturamento.")
            .Produces(StatusCodes.Status200OK);
        fiscal.MapPost("/notas", (HttpContext httpContext, CreateNotaFiscalRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.FiscalManage, service.ObterEmpresaIdDoCliente(request.ClienteId));
            var response = service.CriarNotaFiscal(request);
            return Results.Created($"/fiscal/notas/{response.Id}", ApiResponses.Ok(response));
        })
        .WithSummary("Cria nota fiscal.")
        .WithDescription("Cria uma nota fiscal vinculada a um pedido e a um cliente, validando coerencia entre itens, empresa e dados fiscais.")
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
        fiscal.MapPost("/notas/{notaFiscalId:guid}/autorizar", (HttpContext httpContext, Guid notaFiscalId, AutorizarNotaFiscalRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.FiscalManage, service.ObterEmpresaIdDaNotaFiscal(notaFiscalId));
            return Results.Ok(ApiResponses.Ok(service.AutorizarNotaFiscal(notaFiscalId, request)));
        })
        .WithSummary("Autoriza nota fiscal.")
        .WithDescription("Autoriza a nota fiscal e efetiva impactos operacionais previstos, como faturamento do pedido e baixa do estoque reservado.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
        fiscal.MapPost("/notas/{notaFiscalId:guid}/rejeitar", (HttpContext httpContext, Guid notaFiscalId, RegistrarRejeicaoNotaFiscalRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.FiscalManage, service.ObterEmpresaIdDaNotaFiscal(notaFiscalId));
            return Results.Ok(ApiResponses.Ok(service.RegistrarRejeicaoNotaFiscal(notaFiscalId, request)));
        })
        .WithSummary("Registra rejeicao de nota fiscal.")
        .WithDescription("Registra codigo e mensagem de rejeicao da nota, preservando o historico de processamento fiscal.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
        fiscal.MapPost("/notas/{notaFiscalId:guid}/cancelar", (HttpContext httpContext, Guid notaFiscalId, CancelarNotaFiscalRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.FiscalManage, service.ObterEmpresaIdDaNotaFiscal(notaFiscalId));
            return Results.Ok(ApiResponses.Ok(service.CancelarNotaFiscal(notaFiscalId, request)));
        })
        .WithSummary("Cancela nota fiscal.")
        .WithDescription("Cancela a nota fiscal e pode estornar impactos operacionais conforme as regras do fluxo e o estado atual da nota.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        var integracoes = app.MapGroup("/integracoes").WithTags("Integracoes");
        integracoes.MapGet("/webhooks", (string? origem, string? status, string? eventoId, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarWebhooks(new ConsultarWebhooksRequest(origem, status, eventoId, page ?? 1, pageSize ?? 20)))))
            .WithSummary("Consulta webhooks processados.")
            .WithDescription("Lista o historico operacional de webhooks por origem, status e evento, facilitando suporte e rastreabilidade de integracoes externas.")
            .Produces(StatusCodes.Status200OK);
        integracoes.MapPost("/webhooks", (HttpContext httpContext, ProcessarWebhookRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, IdentityPermissions.IntegracoesManage);
            return Results.Ok(ApiResponses.Ok(service.ProcessarWebhook(request)));
        })
        .WithSummary("Processa webhook.")
        .WithDescription("Recebe um webhook externo, aplica idempotencia e registra o resultado do processamento para posterior consulta operacional.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        return app;
    }

    public static WebApplication UseErpExceptionHandling(this WebApplication app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
                var (statusCode, code, message) = exception switch
                {
                    DomainException domainException => (StatusCodes.Status400BadRequest, "domain_error", domainException.Message),
                    NotFoundException notFoundException => (StatusCodes.Status404NotFound, "not_found", notFoundException.Message),
                    UnauthorizedAccessException unauthorizedAccessException => (StatusCodes.Status401Unauthorized, "unauthorized", unauthorizedAccessException.Message),
                    ForbiddenException forbiddenException => (StatusCodes.Status403Forbidden, "forbidden", forbiddenException.Message),
                    _ => (StatusCodes.Status500InternalServerError, "internal_error", "Erro interno da aplicacao.")
                };

                context.Response.StatusCode = statusCode;
                await context.Response.WriteAsJsonAsync(ApiResponses.Error(code, message));
            });
        });

        return app;
    }

    private static void RequirePermission(HttpContext httpContext, ErpApplicationService service, string permission, Guid? empresaId = null)
    {
        var sessionToken = ResolveSessionToken(httpContext);
        service.ValidarAcesso(sessionToken, permission, empresaId);
    }

    private static string ResolveSessionToken(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue(HeaderNames.Authorization, out var authHeaderValues))
        {
            var bearer = authHeaderValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(bearer) && bearer.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var jwtTokenService = httpContext.RequestServices.GetRequiredService<JwtTokenService>();
                return jwtTokenService.ExtractSessionToken(bearer["Bearer ".Length..].Trim());
            }
        }

        if (httpContext.Request.Headers.TryGetValue(SessionHeader, out var headerValues) && !string.IsNullOrWhiteSpace(headerValues.FirstOrDefault()))
        {
            return headerValues.First()!;
        }

        throw new UnauthorizedAccessException("Authorization Bearer ou header X-Session-Token e obrigatorio.");
    }
}
