namespace SmartenUP.Core.Util.Helper
{
    public class StringHelper
    {

        /// <summary>
        /// Limita ao tamanho máximo (trunc) e coloca tudo em caixa alta (UPPERCASE)
        /// </summary>
        /// <param name="valor"></param>
        /// <param name="tamanho"></param>
        /// <param name="notUpperCaseAll"></param>
        /// <returns></returns>
        public static string Formatar(string valor, int tamanho, bool notUpperCaseAll = false)
        {
            string resultado = string.Empty;

            if (valor.Length > tamanho)
                resultado = valor.Substring(0, tamanho);
            else
                resultado = valor;

            if (!notUpperCaseAll)
                resultado = resultado.ToUpperInvariant();

            return resultado;
        }

        public static string RemoveIncorrectSpaces(string stringWithTwoOrMoreSpace)
        {
            var str = string.Empty;
            for (var i = 0; i < stringWithTwoOrMoreSpace.Length; i++)
                if (stringWithTwoOrMoreSpace[i] == ' ')
                {
                    if ((i + 1 < stringWithTwoOrMoreSpace.Length) && (stringWithTwoOrMoreSpace[i + 1] != ' '))
                    {
                        str = str + stringWithTwoOrMoreSpace[i];
                    }
                }
                else
                    str = str + stringWithTwoOrMoreSpace[i];
            return str.Trim();
        }

    }
}
