using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using PlanYourFriday.Domain.Events;
using PlanYourFriday.Domain.Friends;
using PlanYourFriday.Domain.Groups;
using PlanYourFriday.Domain.Notifications;
using PlanYourFriday.Domain.Users;

#nullable disable

namespace PlanYourFriday.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:auth_provider", "development,google")
                .Annotation("Npgsql:Enum:calendar_provider", "google")
                .Annotation("Npgsql:Enum:connection_status", "active,expired,revoked")
                .Annotation("Npgsql:Enum:event_status", "cancelled,confirmed")
                .Annotation("Npgsql:Enum:friendship_status", "accepted,pending")
                .Annotation("Npgsql:Enum:group_status", "cancelled,planning,scheduled,voting")
                .Annotation("Npgsql:Enum:invitation_status", "accepted,pending,revoked,sent")
                .Annotation("Npgsql:Enum:member_role", "organizer,participant")
                .Annotation("Npgsql:Enum:notification_kind", "deadline_soon,event_cancelled,event_confirmed,event_updated,friend_accepted,friend_request,invited,member_joined,poll_closed,poll_started,voting_started")
                .Annotation("Npgsql:Enum:sync_status", "failed,pending,synced")
                .Annotation("Npgsql:Enum:vote_answer", "maybe,no,yes")
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "data_protection_keys",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    friendly_name = table.Column<string>(type: "text", nullable: true),
                    xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_data_protection_keys", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "citext", nullable: false),
                    display_name = table.Column<string>(type: "citext", nullable: false),
                    time_zone = table.Column<string>(type: "text", nullable: false),
                    username = table.Column<string>(type: "citext", maxLength: 30, nullable: false),
                    friend_code = table.Column<string>(type: "text", nullable: false),
                    photo_url = table.Column<string>(type: "text", nullable: true),
                    session_version = table.Column<int>(type: "integer", nullable: false),
                    settings = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "calendar_connections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<CalendarProvider>(type: "calendar_provider", nullable: false),
                    external_account_id = table.Column<string>(type: "text", nullable: false),
                    account_email = table.Column<string>(type: "text", nullable: false),
                    calendar_id = table.Column<string>(type: "text", nullable: false),
                    busy_calendar_ids = table.Column<string[]>(type: "text[]", nullable: false),
                    last_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    access_token = table.Column<string>(type: "text", nullable: false),
                    refresh_token = table.Column<string>(type: "text", nullable: false),
                    token_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    scopes = table.Column<string[]>(type: "text[]", nullable: false),
                    status = table.Column<ConnectionStatus>(type: "connection_status", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_calendar_connections", x => x.id);
                    table.ForeignKey(
                        name: "fk_calendar_connections_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "friendships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_low_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_high_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requester_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<FriendshipStatus>(type: "friendship_status", nullable: false),
                    accepted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_friendships", x => x.id);
                    table.CheckConstraint("ck_friendships_order", "user_low_id < user_high_id");
                    table.ForeignKey(
                        name: "fk_friendships_users_user_high_id",
                        column: x => x.user_high_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_friendships_users_user_low_id",
                        column: x => x.user_low_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    invite_token = table.Column<string>(type: "text", nullable: false),
                    event_duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    availability_deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<GroupStatus>(type: "group_status", nullable: false),
                    voting_deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    window_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    window_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_groups", x => x.id);
                    table.CheckConstraint("ck_groups_window", "window_end > window_start");
                    table.ForeignKey(
                        name: "fk_groups_users_organizer_id",
                        column: x => x.organizer_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_identities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<AuthProvider>(type: "auth_provider", nullable: false),
                    subject = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_identities", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_identities_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    location = table.Column<string>(type: "text", nullable: true),
                    meeting_link = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<EventStatus>(type: "event_status", nullable: false),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_events", x => x.id);
                    table.CheckConstraint("ck_events_time", "ends_at > starts_at");
                    table.ForeignKey(
                        name: "fk_events_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_events_users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "group_members",
                columns: table => new
                {
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<MemberRole>(type: "member_role", nullable: false),
                    reminded_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_members", x => new { x.group_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_group_members_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_group_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invitations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "citext", nullable: false),
                    status = table.Column<InvitationStatus>(type: "invitation_status", nullable: false),
                    accepted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invitations", x => x.id);
                    table.ForeignKey(
                        name: "fk_invitations_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_invitations_users_accepted_by_user_id",
                        column: x => x.accepted_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "polls",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    multiple_choice = table.Column<bool>(type: "boolean", nullable: false),
                    allow_new_options = table.Column<bool>(type: "boolean", nullable: false),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    closed_notified = table.Column<bool>(type: "boolean", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_polls", x => x.id);
                    table.ForeignKey(
                        name: "fk_polls_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_polls_users_creator_id",
                        column: x => x.creator_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "time_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    free_user_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_time_options", x => x.id);
                    table.CheckConstraint("ck_time_options_time", "ends_at > starts_at");
                    table.ForeignKey(
                        name: "fk_time_options_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "calendar_event_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    connection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_event_id = table.Column<string>(type: "text", nullable: true),
                    sync_status = table.Column<SyncStatus>(type: "sync_status", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_calendar_event_links", x => x.id);
                    table.ForeignKey(
                        name: "fk_calendar_event_links_calendar_connections_connection_id",
                        column: x => x.connection_id,
                        principalTable: "calendar_connections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_calendar_event_links_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "member_availability",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slot_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    slot_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_member_availability", x => x.id);
                    table.CheckConstraint("ck_member_availability_slot", "slot_end > slot_start");
                    table.ForeignKey(
                        name: "fk_member_availability_group_members_group_id_user_id",
                        columns: x => new { x.group_id, x.user_id },
                        principalTable: "group_members",
                        principalColumns: new[] { "group_id", "user_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<NotificationKind>(type: "notification_kind", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    poll_id = table.Column<Guid>(type: "uuid", nullable: true),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_notifications_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_notifications_polls_poll_id",
                        column: x => x.poll_id,
                        principalTable: "polls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_notifications_users_actor_id",
                        column: x => x.actor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "poll_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    poll_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    added_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_poll_options", x => x.id);
                    table.ForeignKey(
                        name: "fk_poll_options_polls_poll_id",
                        column: x => x.poll_id,
                        principalTable: "polls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "poll_participants",
                columns: table => new
                {
                    poll_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_poll_participants", x => new { x.poll_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_poll_participants_polls_poll_id",
                        column: x => x.poll_id,
                        principalTable: "polls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_poll_participants_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "time_option_votes",
                columns: table => new
                {
                    option_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    answer = table.Column<VoteAnswer>(type: "vote_answer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_time_option_votes", x => new { x.option_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_time_option_votes_time_options_option_id",
                        column: x => x.option_id,
                        principalTable: "time_options",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_time_option_votes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "poll_votes",
                columns: table => new
                {
                    option_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_poll_votes", x => new { x.option_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_poll_votes_poll_options_option_id",
                        column: x => x.option_id,
                        principalTable: "poll_options",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_poll_votes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_calendar_connections_user_id_provider_external_account_id",
                table: "calendar_connections",
                columns: new[] { "user_id", "provider", "external_account_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_calendar_event_links_connection_id",
                table: "calendar_event_links",
                column: "connection_id");

            migrationBuilder.CreateIndex(
                name: "ix_calendar_event_links_event_id_connection_id",
                table: "calendar_event_links",
                columns: new[] { "event_id", "connection_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_events_group_id",
                table: "events",
                column: "group_id",
                unique: true,
                filter: "status = 'confirmed'");

            migrationBuilder.CreateIndex(
                name: "ix_events_owner_id",
                table: "events",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_friendships_user_high_id",
                table: "friendships",
                column: "user_high_id");

            migrationBuilder.CreateIndex(
                name: "ix_friendships_user_low_id_user_high_id",
                table: "friendships",
                columns: new[] { "user_low_id", "user_high_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_group_members_user_id",
                table: "group_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_groups_invite_token",
                table: "groups",
                column: "invite_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_groups_organizer_id",
                table: "groups",
                column: "organizer_id");

            migrationBuilder.CreateIndex(
                name: "ix_invitations_accepted_by_user_id",
                table: "invitations",
                column: "accepted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_invitations_group_id_email",
                table: "invitations",
                columns: new[] { "group_id", "email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_member_availability_group_id_user_id",
                table: "member_availability",
                columns: new[] { "group_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_actor_id",
                table: "notifications",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_group_id",
                table: "notifications",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_poll_id",
                table: "notifications",
                column: "poll_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id_created_at",
                table: "notifications",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_next_attempt_at",
                table: "outbox_messages",
                column: "next_attempt_at",
                filter: "processed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_poll_options_poll_id",
                table: "poll_options",
                column: "poll_id");

            migrationBuilder.CreateIndex(
                name: "ix_poll_participants_user_id",
                table: "poll_participants",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_poll_votes_user_id",
                table: "poll_votes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_polls_creator_id",
                table: "polls",
                column: "creator_id");

            migrationBuilder.CreateIndex(
                name: "ix_polls_group_id",
                table: "polls",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_time_option_votes_user_id",
                table: "time_option_votes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_time_options_group_id",
                table: "time_options",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_identities_provider_subject",
                table: "user_identities",
                columns: new[] { "provider", "subject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_identities_user_id",
                table: "user_identities",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_friend_code",
                table: "users",
                column: "friend_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "calendar_event_links");

            migrationBuilder.DropTable(
                name: "data_protection_keys");

            migrationBuilder.DropTable(
                name: "friendships");

            migrationBuilder.DropTable(
                name: "invitations");

            migrationBuilder.DropTable(
                name: "member_availability");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "poll_participants");

            migrationBuilder.DropTable(
                name: "poll_votes");

            migrationBuilder.DropTable(
                name: "time_option_votes");

            migrationBuilder.DropTable(
                name: "user_identities");

            migrationBuilder.DropTable(
                name: "calendar_connections");

            migrationBuilder.DropTable(
                name: "events");

            migrationBuilder.DropTable(
                name: "group_members");

            migrationBuilder.DropTable(
                name: "poll_options");

            migrationBuilder.DropTable(
                name: "time_options");

            migrationBuilder.DropTable(
                name: "polls");

            migrationBuilder.DropTable(
                name: "groups");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
