using ApiPdfCsv.Modules.CodeManagement.Application.Interfaces;
using ApiPdfCsv.Modules.CodeManagement.Application.Services;
using ApiPdfCsv.Modules.CodeManagement.Domain.Entities;
using ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace ApiPdfCsv.Tests.Unit;

public class ImpostoServiceMappingTests
{
    [Fact]
    public async Task MapearDebitoECredito_LoadsImpostosOnce()
    {
        var repository = new Mock<IImpostoRepository>();
        repository
            .Setup(r => r.ObterTodosComCodigosAsync("user-1"))
            .ReturnsAsync(new List<Imposto>
            {
                new Imposto
                {
                    Nome = "INSS",
                    CodigoDebito = new CodigoConta { Codigo = "100" },
                    CodigoCredito = new CodigoConta { Codigo = "200" }
                }
            });

        var service = new ImpostoService(
            repository.Object,
            Mock.Of<ICodigoContaRepository>(),
            Mock.Of<ICodigoContaService>(),
            Mock.Of<IMapper>(),
            new MemoryCache(new MemoryCacheOptions()));

        var historico = new List<string> { "PGTO INSS" };
        var (debitos, creditos) = await service.MapearDebitoECredito(historico, "user-1");

        Assert.Single(debitos);
        Assert.Single(creditos);
        Assert.Equal(100m, debitos[0]);
        Assert.Equal(200m, creditos[0]);
        repository.Verify(r => r.ObterTodosComCodigosAsync("user-1"), Times.Once);
    }
}
