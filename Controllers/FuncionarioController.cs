using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using TrilhaNetAzureDesafio.Context;
using TrilhaNetAzureDesafio.Models;

namespace TrilhaNetAzureDesafio.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FuncionarioController : ControllerBase
    {
        private readonly RHContext _context;
        private readonly string _connectionString;
        private readonly string _tableName;

        public FuncionarioController(RHContext context, IConfiguration configuration)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _connectionString = configuration.GetValue<string>("ConnectionStrings:SAConnectionString");
            _tableName = configuration.GetValue<string>("ConnectionStrings:AzureTableName");
        }

        private TableClient GetTableClient()
        {
            var serviceClient = new TableServiceClient(_connectionString);
            var tableClient = serviceClient.GetTableClient(_tableName);

            tableClient.CreateIfNotExists();
            return tableClient;
        }

        [HttpGet("{id}")]
        public IActionResult ObterPorId(int id)
        {
            var funcionario = _context.Funcionarios.Find(id);

            if (funcionario == null)
                return NotFound();

            return Ok(funcionario);
        }

        [HttpPost]
        public IActionResult Criar(Funcionario funcionario)
        {
            _context.Funcionarios.Add(funcionario);

            try
            {
                _context.SaveChanges(); 

                var tableClient = GetTableClient();
                var funcionarioLog = new FuncionarioLog(funcionario, TipoAcao.Inclusao, funcionario.Departamento, Guid.NewGuid().ToString());
                tableClient.UpsertEntity(funcionarioLog); // 
            }
            catch (Exception ex)
            {
            
                return StatusCode(500, $"Error: {ex.Message}");
            }

            return CreatedAtAction(nameof(ObterPorId), new { id = funcionario.Id }, funcionario);
        }

        [HttpPut("{id}")]
        public IActionResult Atualizar(int id, Funcionario funcionario)
        {
            var funcionarioBanco = _context.Funcionarios.Find(id);

            if (funcionarioBanco == null)
                return NotFound();

            funcionarioBanco.Nome = funcionario.Nome;
            funcionarioBanco.Endereco = funcionario.Endereco;

            try
            {
                _context.SaveChanges(); 

                var tableClient = GetTableClient();
                var funcionarioLog = new FuncionarioLog(funcionarioBanco, TipoAcao.Atualizacao, funcionarioBanco.Departamento, Guid.NewGuid().ToString());
                tableClient.UpsertEntity(funcionarioLog); // 
            }
            catch (Exception ex)
            {
                
                return StatusCode(500, $"Error: {ex.Message}");
            }

            return Ok();
        }

        [HttpDelete("{id}")]
        public IActionResult Deletar(int id)
        {
            var funcionarioBanco = _context.Funcionarios.Find(id);

            if (funcionarioBanco == null)
                return NotFound();

            try
            {
                _context.Funcionarios.Remove(funcionarioBanco);
                _context.SaveChanges(); 

                var tableClient = GetTableClient();
                var funcionarioLog = new FuncionarioLog(funcionarioBanco, TipoAcao.Remocao, funcionarioBanco.Departamento, Guid.NewGuid().ToString());
                tableClient.UpsertEntity(funcionarioLog); // 
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }

            return NoContent();
        }
    }
}
