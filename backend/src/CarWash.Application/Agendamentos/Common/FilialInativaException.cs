using CarWash.Application.Common.Exceptions;

namespace CarWash.Application.Agendamentos.Common;

/// <summary>
/// RF019: a filial selecionada existe mas está inativa e não pode receber novos
/// agendamentos. Herda de <see cref="ConflictException"/> → 409 + slug
/// "filial-inativa" via <c>ExceptionHandlingMiddleware</c> (caminho genérico de
/// <see cref="ConflictException"/> já existente — sem novo catch). Espelha a
/// receita de <see cref="CapacidadeFilialEsgotadaException"/>. Diferente de
/// <c>RecursoInativoException</c> (422), que segue cobrindo veículo/cliente/
/// serviço/responsável inativos.
/// </summary>
#pragma warning disable RCS1194 // Exceção semântica; os construtores cobrem os usos reais.
public sealed class FilialInativaException : ConflictException
#pragma warning restore RCS1194
{
    public const string MensagemPadrao = MensagensFilialAgendamento.Inativa;

    public const string SlugPadrao = "filial-inativa";

    public FilialInativaException()
        : base(MensagemPadrao, SlugPadrao)
    {
    }

    public FilialInativaException(Exception innerException)
        : base(MensagemPadrao, SlugPadrao, innerException)
    {
    }
}
