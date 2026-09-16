using ClickYa.Api.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ClickYa.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260916000100_CompletarServiciosUrgencias")]
public partial class CompletarServiciosUrgencias : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<int>(
            name: "TecnicoId", table: "Urgencias", type: "integer", nullable: true,
            oldClrType: typeof(int), oldType: "integer");

        migrationBuilder.AddColumn<int>(name: "CategoriaId", table: "Tecnicos", type: "integer", nullable: true);
        migrationBuilder.AddColumn<int>(name: "CategoriaId", table: "Urgencias", type: "integer", nullable: true);
        migrationBuilder.AddColumn<string>(name: "ZonaBarrio", table: "Urgencias", type: "text", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "DireccionExactaProtegida", table: "Urgencias", type: "text", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "ContactoClienteProtegido", table: "Urgencias", type: "text", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "TokenClienteHash", table: "Urgencias", type: "text", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<int>(name: "OfertaSeleccionadaId", table: "Urgencias", type: "integer", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "FechaSeleccion", table: "Urgencias", type: "timestamp with time zone", nullable: true);

        migrationBuilder.Sql("""
            UPDATE "Tecnicos" t
            SET "CategoriaId" = c."Id"
            FROM "Categorias" c
            WHERE lower(trim(c."Seccion")) = 'servicios'
              AND lower(trim(c."Nombre")) = lower(trim(t."Rubro"));

            UPDATE "Urgencias" u
            SET "CategoriaId" = c."Id"
            FROM "Categorias" c
            WHERE lower(trim(c."Seccion")) = 'servicios'
              AND lower(trim(c."Nombre")) = lower(trim(u."Rubro"));
            """);

        migrationBuilder.CreateTable(
            name: "OfertasUrgencia",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                UrgenciaId = table.Column<int>(type: "integer", nullable: false),
                TecnicoId = table.Column<int>(type: "integer", nullable: false),
                PrecioEstimado = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                Disponibilidad = table.Column<string>(type: "text", nullable: false),
                Mensaje = table.Column<string>(type: "text", nullable: false),
                FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OfertasUrgencia", x => x.Id);
                table.CheckConstraint("CK_OfertasUrgencia_Precio", "\"PrecioEstimado\" >= 0");
                table.ForeignKey("FK_OfertasUrgencia_Tecnicos_TecnicoId", x => x.TecnicoId, "Tecnicos", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_OfertasUrgencia_Urgencias_UrgenciaId", x => x.UrgenciaId, "Urgencias", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "CalificacionesServicio",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                UrgenciaId = table.Column<int>(type: "integer", nullable: false),
                TecnicoId = table.Column<int>(type: "integer", nullable: false),
                Estrellas = table.Column<int>(type: "integer", nullable: false),
                Comentario = table.Column<string>(type: "text", nullable: true),
                FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CalificacionesServicio", x => x.Id);
                table.CheckConstraint("CK_CalificacionesServicio_Estrellas", "\"Estrellas\" BETWEEN 1 AND 5");
                table.ForeignKey("FK_CalificacionesServicio_Tecnicos_TecnicoId", x => x.TecnicoId, "Tecnicos", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_CalificacionesServicio_Urgencias_UrgenciaId", x => x.UrgenciaId, "Urgencias", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_Tecnicos_CategoriaId", table: "Tecnicos", column: "CategoriaId");
        migrationBuilder.CreateIndex(name: "IX_Urgencias_CategoriaId", table: "Urgencias", column: "CategoriaId");
        migrationBuilder.CreateIndex(name: "IX_Urgencias_OfertaSeleccionadaId", table: "Urgencias", column: "OfertaSeleccionadaId", unique: true);
        migrationBuilder.CreateIndex(name: "IX_OfertasUrgencia_TecnicoId", table: "OfertasUrgencia", column: "TecnicoId");
        migrationBuilder.CreateIndex(name: "IX_OfertasUrgencia_UrgenciaId_TecnicoId", table: "OfertasUrgencia", columns: new[] { "UrgenciaId", "TecnicoId" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_CalificacionesServicio_TecnicoId", table: "CalificacionesServicio", column: "TecnicoId");
        migrationBuilder.CreateIndex(name: "IX_CalificacionesServicio_UrgenciaId", table: "CalificacionesServicio", column: "UrgenciaId", unique: true);

        migrationBuilder.AddForeignKey(name: "FK_Tecnicos_Categorias_CategoriaId", table: "Tecnicos", column: "CategoriaId", principalTable: "Categorias", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_Urgencias_Categorias_CategoriaId", table: "Urgencias", column: "CategoriaId", principalTable: "Categorias", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_Urgencias_OfertasUrgencia_OfertaSeleccionadaId", table: "Urgencias", column: "OfertaSeleccionadaId", principalTable: "OfertasUrgencia", principalColumn: "Id", onDelete: ReferentialAction.Restrict);

        // Los datos históricos no se bloquean: la FK controla toda alta o modificación nueva.
        migrationBuilder.Sql("""
            ALTER TABLE "Urgencias"
            ADD CONSTRAINT "FK_Urgencias_Tecnicos_TecnicoId"
            FOREIGN KEY ("TecnicoId") REFERENCES "Tecnicos" ("Id") ON DELETE SET NULL NOT VALID;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE \"Urgencias\" DROP CONSTRAINT IF EXISTS \"FK_Urgencias_Tecnicos_TecnicoId\";");
        migrationBuilder.DropForeignKey("FK_Tecnicos_Categorias_CategoriaId", "Tecnicos");
        migrationBuilder.DropForeignKey("FK_Urgencias_Categorias_CategoriaId", "Urgencias");
        migrationBuilder.DropForeignKey("FK_Urgencias_OfertasUrgencia_OfertaSeleccionadaId", "Urgencias");
        migrationBuilder.DropTable("CalificacionesServicio");
        migrationBuilder.DropTable("OfertasUrgencia");
        migrationBuilder.DropIndex("IX_Tecnicos_CategoriaId", "Tecnicos");
        migrationBuilder.DropIndex("IX_Urgencias_CategoriaId", "Urgencias");
        migrationBuilder.DropColumn("CategoriaId", "Tecnicos");
        migrationBuilder.DropColumn("CategoriaId", "Urgencias");
        migrationBuilder.DropColumn("ZonaBarrio", "Urgencias");
        migrationBuilder.DropColumn("DireccionExactaProtegida", "Urgencias");
        migrationBuilder.DropColumn("ContactoClienteProtegido", "Urgencias");
        migrationBuilder.DropColumn("TokenClienteHash", "Urgencias");
        migrationBuilder.DropColumn("OfertaSeleccionadaId", "Urgencias");
        migrationBuilder.DropColumn("FechaSeleccion", "Urgencias");
        migrationBuilder.AlterColumn<int>(name: "TecnicoId", table: "Urgencias", type: "integer", nullable: false, defaultValue: 0, oldClrType: typeof(int), oldType: "integer", oldNullable: true);
    }
}
