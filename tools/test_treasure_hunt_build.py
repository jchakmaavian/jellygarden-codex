"""Authoring regressions: broken area budgets and unavailable content must fail closed."""
import copy
import json
import unittest
from unittest import mock
import treasure_hunt_build as campaign


class CampaignTests(unittest.TestCase):
    def setUp(self):
        self.source = json.loads(campaign.SOURCE.read_text())

    def rejects(self, mutation, message):
        source = copy.deepcopy(self.source)
        mutation(source)
        with self.assertRaisesRegex(AssertionError, message):
            campaign.compile_campaign(source)

    def test_compilation_is_deterministic(self):
        first = campaign.compile_campaign(self.source)
        self.assertEqual(first, campaign.compile_campaign(self.source))
        self.assertEqual(first['campaign.json']['readyAreas'], [1])
        self.assertEqual(first['level_plan.json']['byArea'], {'1': [1, 2, 3, 4]})
        self.assertEqual(first['index.json']['totalStars'], 4)

    def test_underfunded_tasks(self):
        self.rejects(lambda s: s['areas'][0]['boardNumbers'].pop(), 'Unfunded tasks')

    def test_task_dialogue_requires_a_matching_speaker(self):
        self.rejects(lambda s: s['areas'][0]['tasks'][1].update(onComplete='Finished!'),
                     'Task dialogue needs both speaker and line, or neither')
        self.rejects(lambda s: s['areas'][0]['tasks'][1].update(speaker='Leo'),
                     'Task dialogue needs both speaker and line, or neither')

    def test_optional_task_dialogue_survives_compilation(self):
        self.source['areas'][0]['tasks'][1].update(speaker='Leo', onComplete='Finished!')
        tasks = campaign.compile_campaign(self.source)['arc1.json']['areas'][0]['tasks']
        self.assertEqual(tasks[0]['onComplete'], '')
        self.assertEqual(tasks[1]['onComplete'], 'Finished!')

    def test_non_speaking_character_cannot_have_task_dialogue(self):
        self.rejects(lambda s: s['areas'][0]['tasks'][1].update(speaker='Mimi', onComplete='Finished!'),
                     'Non-speaking character')

    def test_duplicate_first_win(self):
        self.rejects(lambda s: s['areas'][0]['boardNumbers'].__setitem__(3, 1), 'Duplicate rewardable level')

    def test_task_order(self):
        self.rejects(lambda s: s['areas'][0]['tasks'][1].update(index=3), 'Task order')

    def test_transition_cannot_be_instant(self):
        self.rejects(lambda s: s['areas'][0]['tasks'][0].update(instantRepair=True),
                     'A transition cannot be instant')

    def test_transition_must_be_supported(self):
        self.rejects(lambda s: s['areas'][0]['tasks'][0].update(repairTransition='vine-typo'),
                     'Unknown repair transition')

    def test_transition_requires_its_own_leaf_anchors(self):
        self.rejects(lambda s: s['areas'][0]['tasks'][1].update(repairTransition='vines'),
                     'Missing vine transition anchors')

    def test_gate_transition_requires_baked_layers(self):
        exists = campaign.Path.exists
        with mock.patch.object(campaign.Path, 'exists',
                               lambda p: False if p.name.endswith('_parts_layout.json') else exists(p)):
            with self.assertRaisesRegex(AssertionError, 'Missing gate transition parts'):
                campaign.compile_campaign(copy.deepcopy(self.source))

    def test_tuning_only_owned_boards(self):
        self.rejects(lambda s: s['areas'][0]['minimumMoves'].update({'5': 20}), 'Move tuning for unassigned board')

    def test_missing_payoff(self):
        self.rejects(lambda s: s['areas'][0].update(payoffScene='missing'), '')

    def test_non_speaking_cat(self):
        self.rejects(lambda s: s['areas'][0]['scenes'][0]['lines'][0].update(speaker='Mimi'), 'Non-speaking character')

    def test_invalid_art_crop(self):
        self.rejects(lambda s: s['artAliases'][-1].update(rect=[0.9, 0, 0.5, 1]), 'Invalid art crop')

    def test_ready_area_requires_task_art(self):
        self.rejects(lambda s: s.update(artAliases=[a for a in s['artAliases'] if a['key'] != 'task_001_1']),
                     'Missing required detail art')

    def test_catalogue_cannot_skip_successor(self):
        self.rejects(lambda s: s['areas'][0].update(pointsTo=3), 'Invalid next area')


if __name__ == '__main__':
    unittest.main()
