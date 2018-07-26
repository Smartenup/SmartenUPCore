using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Services.Common;
using Nop.Services.Localization;
using System;

namespace SmartenUP.Core.Util.Helper
{
    public class AddressHelper
    {
        private const int PHONE_WITHOUT_AREA_CODE_MAX_LENGTH = 9;
        private const int PHONE_ONLY_NUMBER_ONE_MAX_LENGTH = 11;


        private readonly IAddressAttributeParser _addressAttributeParser;
        private readonly IWorkContext _workContext;

        public AddressHelper(IAddressAttributeParser addressAttributeParser,
            IWorkContext workContext)
        {
            _workContext = workContext;
            _addressAttributeParser = addressAttributeParser;
        }

        public static string FormatarCEPHiffen(string cep)
        {
            string cepFormatado = NumberHelper.ObterApenasNumeros(cep);

            if (cepFormatado.Length == 8)
            {
                cepFormatado = string.Concat(cepFormatado.Substring(0, 5), "-", cepFormatado.Substring(5, 3));
            }

            return cepFormatado;
        }

        public void GetCustomNumberAndComplement(string customAttributes, out string number, out string complement)
        {
            number = string.Empty;
            complement = string.Empty;

            if (!string.IsNullOrWhiteSpace(customAttributes))
            {
                var attributes = _addressAttributeParser.ParseAddressAttributes(customAttributes);

                for (var i = 0; i < attributes.Count; i++)
                {
                    var valuesStr = _addressAttributeParser.ParseValues(customAttributes, attributes[i].Id);

                    var attributeName = attributes[i].GetLocalized(a => a.Name, _workContext.WorkingLanguage.Id);

                    if (
                        attributeName.Equals("Número", StringComparison.InvariantCultureIgnoreCase) ||
                        attributeName.Equals("Numero", StringComparison.InvariantCultureIgnoreCase)
                        )
                    {
                        number = _addressAttributeParser.ParseValues(customAttributes, attributes[i].Id)[0];
                    }

                    if (attributeName.Equals("Complemento", StringComparison.InvariantCultureIgnoreCase))
                        complement = _addressAttributeParser.ParseValues(customAttributes, attributes[i].Id)[0];
                }
            }

            if (string.IsNullOrWhiteSpace(number))
                number = "--";

            if (string.IsNullOrWhiteSpace(complement))
                complement = "--";
        }


        public void GetCustomNumberAndComplement(string customAttributes, out string number, out string complement, out string cnpjcpf)
        {
            GetCustomNumberAndComplement(customAttributes, out number, out complement);

            cnpjcpf = string.Empty;

            if (string.IsNullOrWhiteSpace(customAttributes))
            {
                return;
            }

            var attributes = _addressAttributeParser.ParseAddressAttributes(customAttributes);

            for (int i = 0; i < attributes.Count; i++)
            {
                var valuesStr = _addressAttributeParser.ParseValues(customAttributes, attributes[i].Id);

                var attributeName = attributes[i].GetLocalized(a => a.Name, _workContext.WorkingLanguage.Id);

                if (attributeName.Equals("CPF/CNPJ", StringComparison.InvariantCultureIgnoreCase))
                    cnpjcpf = _addressAttributeParser.ParseValues(customAttributes, attributes[i].Id)[0];
            }

            if (!string.IsNullOrWhiteSpace(cnpjcpf))
                cnpjcpf = NumberHelper.ObterApenasNumeros(cnpjcpf);
        }


        public static string FormatarCelular(string telefone)
        {
            string numero = string.Empty;
            string codigoArea = "00";
            string retorno = string.Empty;


            var phoneNumber = NumberHelper.ObterApenasNumeros(telefone);

            phoneNumber = RemoverCodigoAreaIncorreto(phoneNumber);

            ///Se o numero de telefone não conter o código de área
            if (phoneNumber.Length <= PHONE_WITHOUT_AREA_CODE_MAX_LENGTH)
            {
                numero = phoneNumber;
            }
            ///Se o numero de telefone conter o código de área e for menor que o tamanho maximo para um telefone
            else if ((phoneNumber.Length > PHONE_WITHOUT_AREA_CODE_MAX_LENGTH) &&
                     (phoneNumber.Length <= PHONE_ONLY_NUMBER_ONE_MAX_LENGTH))
            {

                codigoArea = phoneNumber.Substring(0, 2);
                numero = phoneNumber.Substring(2);
            }

            while (codigoArea.Length != 2)
                codigoArea = "0" + codigoArea;

            while (numero.Length != 9)
                numero = "0" + numero;

            retorno = codigoArea + numero;

            return retorno;
        }

        public static string ObterNumeroTelefone(string telefoneFormatadoApenasNumeros)
        {
            var phoneNumber = NumberHelper.ObterApenasNumeros(telefoneFormatadoApenasNumeros);

            string numero = string.Empty;
            string retorno = string.Empty;

            if (phoneNumber.Length <= PHONE_WITHOUT_AREA_CODE_MAX_LENGTH)
            {
                numero = phoneNumber;
            }
            else if ((phoneNumber.Length > PHONE_WITHOUT_AREA_CODE_MAX_LENGTH) &&
                    (phoneNumber.Length <= PHONE_ONLY_NUMBER_ONE_MAX_LENGTH))
            {
                numero = phoneNumber.Substring(2);
            }

            while (numero.Length != PHONE_WITHOUT_AREA_CODE_MAX_LENGTH)
                numero = "0" + numero;

            retorno = numero;

            return retorno;
        }

        public static string ObterAreaTelefone(string telefoneFormatadoApenasNumeros)
        {

            var phoneNumber = NumberHelper.ObterApenasNumeros(telefoneFormatadoApenasNumeros);

            string codigoArea = string.Empty;
            string retorno = string.Empty;

            if (phoneNumber.Length <= PHONE_WITHOUT_AREA_CODE_MAX_LENGTH)
            {
                codigoArea = "00";
            }
            else if ((phoneNumber.Length > PHONE_WITHOUT_AREA_CODE_MAX_LENGTH) &&
                    (phoneNumber.Length <= PHONE_ONLY_NUMBER_ONE_MAX_LENGTH))
            {
                codigoArea = phoneNumber.Substring(0, 2);
            }

            while (codigoArea.Length != 2)
                codigoArea = "0" + codigoArea;

            retorno = codigoArea;

            return retorno;
        }


        public static string RemoverCodigoAreaIncorreto(string telefoneCompleto)
        {
            if (telefoneCompleto.Length > PHONE_WITHOUT_AREA_CODE_MAX_LENGTH)
            {
                if (telefoneCompleto.StartsWith("0"))
                {
                    telefoneCompleto = telefoneCompleto.Substring(1);
                }

                ///TODO:Validar os codigo de áreas validos e caso não for retirar do telefone
            }

            return telefoneCompleto;
        }

        public static string GetFullName(Address address, int length = 50)
        {
            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            var firstName = address.FirstName;
            var lastName = address.LastName;
            var stringWithTwoOrMoreSpace = "";

            if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
            {
                stringWithTwoOrMoreSpace = string.Format("{0} {1}", firstName, lastName);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(firstName))
                {
                    stringWithTwoOrMoreSpace = firstName;
                }
                if (!string.IsNullOrWhiteSpace(lastName))
                {
                    stringWithTwoOrMoreSpace = lastName;
                }
            }

            var billingShippingFullName = StringHelper.RemoveIncorrectSpaces(stringWithTwoOrMoreSpace);

            if (billingShippingFullName.Length > length)
            {
                billingShippingFullName = billingShippingFullName.Substring(0, length);
            }

            return billingShippingFullName;
        }
    }
}
