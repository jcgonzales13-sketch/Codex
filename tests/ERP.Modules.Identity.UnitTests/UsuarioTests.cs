using ERP.BuildingBlocks;
using ERP.Modules.Identity;

namespace ERP.Modules.Identity.UnitTests;

public sealed class UsuarioTests
{
    [Fact]
    public void Deve_impedir_cadastro_com_email_duplicado_na_mesma_empresa()
    {
        var repository = new FakeUsuarioRepository(emailJaExiste: true);
        var service = new CadastroUsuarioService(repository);

        var exception = Assert.Throws<DomainException>(() =>
            service.Cadastrar(Guid.NewGuid(), "usuario@empresa.com", "Maria"));

        Assert.Equal("Ja existe usuario cadastrado com este email na empresa.", exception.Message);
    }

    [Fact]
    public void Deve_exigir_usuario_ativo_para_conceder_permissao()
    {
        var usuario = new Usuario(Guid.NewGuid(), "usuario@empresa.com", "Maria");

        var exception = Assert.Throws<DomainException>(() => usuario.ConcederPermissao("PEDIDO_APROVAR"));

        Assert.Equal("Somente usuario ativo pode receber permissao.", exception.Message);
    }

    [Fact]
    public void Deve_bloquear_reativacao_direta_de_usuario_bloqueado()
    {
        var usuario = new Usuario(Guid.NewGuid(), "usuario@empresa.com", "Maria");
        usuario.Ativar();
        usuario.Bloquear("Tentativas invalidas");

        var exception = Assert.Throws<DomainException>(() => usuario.Ativar());

        Assert.Equal("Usuario bloqueado nao pode ser ativado sem desbloqueio.", exception.Message);
    }

    [Fact]
    public void Deve_configurar_senha_e_validar_credencial()
    {
        var usuario = new Usuario(Guid.NewGuid(), "usuario@empresa.com", "Maria");

        usuario.DefinirSenha("Senha@123", ativarUsuario: true);

        Assert.True(usuario.PossuiSenhaConfigurada);
        Assert.True(usuario.ValidarSenha("Senha@123"));
        Assert.False(usuario.ValidarSenha("SenhaErrada"));
        Assert.Equal(StatusUsuario.Ativo, usuario.Status);
    }

    [Fact]
    public void Deve_rejeitar_senha_curta()
    {
        var usuario = new Usuario(Guid.NewGuid(), "usuario@empresa.com", "Maria");

        var exception = Assert.Throws<DomainException>(() => usuario.DefinirSenha("123", ativarUsuario: false));

        Assert.Equal("Senha deve possuir ao menos 8 caracteres.", exception.Message);
    }

    [Fact]
    public void Deve_permitir_vincular_perfil_a_usuario_ativo()
    {
        var usuario = new Usuario(Guid.NewGuid(), "usuario@empresa.com", "Maria");
        usuario.Ativar();
        var perfil = new PerfilAcesso(usuario.EmpresaId, "Operador", ["ESTOQUE_MANAGE"]);

        usuario.VincularPerfil(perfil.Id);

        Assert.Contains(perfil.Id, usuario.PerfisAcesso);
    }

    [Fact]
    public void Deve_exigir_ao_menos_uma_permissao_no_perfil_de_acesso()
    {
        var exception = Assert.Throws<DomainException>(() => new PerfilAcesso(Guid.NewGuid(), "Operador", []));

        Assert.Equal("Perfil de acesso deve possuir ao menos uma permissao.", exception.Message);
    }

    private sealed class FakeUsuarioRepository(bool emailJaExiste) : IUsuarioRepository
    {
        public bool EmailJaExiste(Guid empresaId, string email) => emailJaExiste;

        public void Add(Usuario usuario)
        {
        }
    }
}
