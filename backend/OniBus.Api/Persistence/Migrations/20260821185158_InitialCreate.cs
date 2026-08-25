using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OniBus.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rotas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    origem = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    destino = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    duracao_estimada = table.Column<TimeSpan>(type: "interval", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rotas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "viagens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rota_id = table.Column<Guid>(type: "uuid", nullable: false),
                    partida = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    preco_base = table.Column<decimal>(type: "numeric(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_viagens", x => x.id);
                    table.ForeignKey(
                        name: "FK_viagens_rotas_rota_id",
                        column: x => x.rota_id,
                        principalTable: "rotas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reservas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    viagem_id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero_assento = table.Column<int>(type: "integer", nullable: false),
                    codigo = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reservas", x => x.id);
                    table.ForeignKey(
                        name: "FK_reservas_viagens_viagem_id",
                        column: x => x.viagem_id,
                        principalTable: "viagens",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_reservas_codigo",
                table: "reservas",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_reservas_viagem_assento_confirmada",
                table: "reservas",
                columns: new[] { "viagem_id", "numero_assento" },
                unique: true,
                filter: "status = 'Confirmada'");

            migrationBuilder.CreateIndex(
                name: "ux_rotas_origem_destino",
                table: "rotas",
                columns: new[] { "origem", "destino" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_viagens_rota_id",
                table: "viagens",
                column: "rota_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reservas");

            migrationBuilder.DropTable(
                name: "viagens");

            migrationBuilder.DropTable(
                name: "rotas");
        }
    }
}
