namespace CarWash.Application.Common;

public static class DocumentoValidator
{
    public static bool CpfValido(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
        {
            return false;
        }

        cpf = InputNormalizer.OnlyDigitsOrNull(cpf);

        if (cpf is null || cpf.Length != 11)
        {
            return false;
        }

        if (cpf.Distinct().Count() == 1)
        {
            return false;
        }

        var soma = 0;

        for (var i = 0; i < 9; i++)
        {
            soma += (cpf[i] - '0') * (10 - i);
        }

        var resto = soma % 11;
        var digito1 = resto < 2 ? 0 : 11 - resto;

        if (digito1 != cpf[9] - '0')
        {
            return false;
        }

        soma = 0;

        for (var i = 0; i < 10; i++)
        {
            soma += (cpf[i] - '0') * (11 - i);
        }

        resto = soma % 11;
        var digito2 = resto < 2 ? 0 : 11 - resto;

        return digito2 == cpf[10] - '0';
    }

    public static bool CnpjValido(string? cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj))
        {
            return false;
        }

        cnpj = InputNormalizer.OnlyDigitsOrNull(cnpj);

        if (cnpj is null || cnpj.Length != 14)
        {
            return false;
        }

        if (cnpj.Distinct().Count() == 1)
        {
            return false;
        }

        int[] peso1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] peso2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

        var soma = 0;

        for (var i = 0; i < 12; i++)
        {
            soma += (cnpj[i] - '0') * peso1[i];
        }

        var resto = soma % 11;
        var digito1 = resto < 2 ? 0 : 11 - resto;

        if (digito1 != cnpj[12] - '0')
        {
            return false;
        }

        soma = 0;

        for (var i = 0; i < 13; i++)
        {
            soma += (cnpj[i] - '0') * peso2[i];
        }

        resto = soma % 11;
        var digito2 = resto < 2 ? 0 : 11 - resto;

        return digito2 == cnpj[13] - '0';
    }
}
